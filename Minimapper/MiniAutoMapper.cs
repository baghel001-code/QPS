// =====================================================================
//  MiniMapper — a small, self-written, AutoMapper-compatible mapper.
//
//  Supports the API surface used in typical profiles:
//    Profile, CreateMap<S,D>(), ForMember(..., opt => opt.MapFrom(..)),
//    opt.Ignore(), opt.ConvertUsing<TConverter, TMember>(..),
//    IValueConverter<S,D> (with constructor DI), ReverseMap(),
//    type-level ConvertUsing(Func<S,D>), convention mapping by name
//    (case-insensitive), nested maps, lists/arrays, nullable, enums,
//    IMapper.Map<T>(obj), Map<S,D>(src), Map(src, existingDest),
//    services.AddMiniMapper(assemblies), AssertConfigurationIsValid().
//
//  Getters/setters are compiled expression trees and every plan is
//  cached, so after the first call per type pair it runs close to
//  hand-written speed.
// =====================================================================

using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MiniMapper
{
    // -----------------------------------------------------------------
    // Public API
    // -----------------------------------------------------------------

    public interface IMapper
    {
        TDestination Map<TDestination>(object? source);
        TDestination Map<TSource, TDestination>(TSource source);
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
        object? Map(object? source, Type sourceType, Type destinationType);
    }

    public interface IValueConverter<in TSourceMember, out TDestinationMember>
    {
        TDestinationMember Convert(TSourceMember sourceMember, ResolutionContext context);
    }

    public sealed class ResolutionContext
    {
        internal ResolutionContext(IMapper mapper, IServiceProvider? services)
        {
            Mapper = mapper;
            Services = services;
        }

        public IMapper Mapper { get; }
        public IServiceProvider? Services { get; }
    }

    public class MiniMapperException : Exception
    {
        public MiniMapperException(string message, Exception? inner = null) : base(message, inner) { }
    }

    // -----------------------------------------------------------------
    // Profile + fluent configuration
    // -----------------------------------------------------------------

    public abstract class Profile
    {
        internal List<TypeMapConfig> TypeMaps { get; } = new();

        protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            var cfg = new TypeMapConfig(typeof(TSource), typeof(TDestination));
            TypeMaps.Add(cfg);
            return new MappingExpression<TSource, TDestination>(cfg, this);
        }
    }

    public interface IMappingExpression<TSource, TDestination>
    {
        IMappingExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> options);

        IMappingExpression<TDestination, TSource> ReverseMap();

        void ConvertUsing(Func<TSource, TDestination> converter);
    }

    public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
    {
        void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);
        void Ignore();
        void ConvertUsing<TValueConverter, TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
            where TValueConverter : IValueConverter<TSourceMember, TMember>;
    }

    internal sealed class TypeMapConfig
    {
        public TypeMapConfig(Type source, Type destination)
        {
            SourceType = source;
            DestinationType = destination;
        }

        public Type SourceType { get; }
        public Type DestinationType { get; }
        public Dictionary<string, MemberConfig> Members { get; } = new(StringComparer.Ordinal);
        public Func<object, object?>? TypeConverter { get; set; }
        public TypeMapConfig? ReverseOf { get; set; }
    }

    internal sealed class MemberConfig
    {
        public required string DestinationName { get; init; }
        public bool Ignored { get; set; }
        public Func<object, object?>? SourceGetter { get; set; }
        public string? SimpleSourceMemberName { get; set; }   // set when MapFrom is "src => src.Prop" (reversible)
        public Type? ValueConverterType { get; set; }
        public Func<object, object?, ResolutionContext, object?>? ValueConverterInvoker { get; set; }
    }

    internal sealed class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
    {
        private readonly TypeMapConfig _cfg;
        private readonly Profile _profile;

        public MappingExpression(TypeMapConfig cfg, Profile profile)
        {
            _cfg = cfg;
            _profile = profile;
        }

        public IMappingExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> options)
        {
            var name = ExpressionHelper.GetMemberName(destinationMember);
            if (!_cfg.Members.TryGetValue(name, out var member))
            {
                member = new MemberConfig { DestinationName = name };
                _cfg.Members[name] = member;
            }
            options(new MemberConfigurationExpression<TSource, TDestination, TMember>(member));
            return this;
        }

        public IMappingExpression<TDestination, TSource> ReverseMap()
        {
            var reverse = new TypeMapConfig(typeof(TDestination), typeof(TSource)) { ReverseOf = _cfg };
            _profile.TypeMaps.Add(reverse);
            return new MappingExpression<TDestination, TSource>(reverse, _profile);
        }

        public void ConvertUsing(Func<TSource, TDestination> converter)
            => _cfg.TypeConverter = src => converter((TSource)src);
    }

    internal sealed class MemberConfigurationExpression<TSource, TDestination, TMember>
        : IMemberConfigurationExpression<TSource, TDestination, TMember>
    {
        private readonly MemberConfig _member;

        public MemberConfigurationExpression(MemberConfig member) => _member = member;

        public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
        {
            _member.SourceGetter = ExpressionHelper.CompileNullSafeGetter(sourceMember);
            _member.SimpleSourceMemberName = ExpressionHelper.TryGetSimpleMemberName(sourceMember);
        }

        public void Ignore() => _member.Ignored = true;

        public void ConvertUsing<TValueConverter, TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
            where TValueConverter : IValueConverter<TSourceMember, TMember>
        {
            _member.SourceGetter = ExpressionHelper.CompileNullSafeGetter(sourceMember);
            _member.ValueConverterType = typeof(TValueConverter);
            _member.ValueConverterInvoker = (converter, value, ctx) =>
                ((IValueConverter<TSourceMember, TMember>)converter).Convert((TSourceMember)value!, ctx);
        }
    }

    // -----------------------------------------------------------------
    // Configuration (built once, singleton)
    // -----------------------------------------------------------------

    public sealed class MapperConfiguration
    {
        internal Dictionary<(Type, Type), TypeMapConfig> TypeMaps { get; } = new();

        public MapperConfiguration(IEnumerable<Profile> profiles)
        {
            var all = profiles.SelectMany(p => p.TypeMaps).ToList();

            // Forward maps first, then reverse maps (so reverse can read the forward config).
            foreach (var map in all.Where(m => m.ReverseOf == null))
                TypeMaps[(map.SourceType, map.DestinationType)] = map;

            foreach (var map in all.Where(m => m.ReverseOf != null))
            {
                BuildReverseMembers(map);
                // An explicit CreateMap in the other direction wins over a ReverseMap.
                TypeMaps.TryAdd((map.SourceType, map.DestinationType), map);
            }
        }

        public static MapperConfiguration FromAssemblies(params Assembly[] assemblies)
        {
            var profiles = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .Select(t => (Profile)Activator.CreateInstance(t)!);
            return new MapperConfiguration(profiles);
        }

        // For forward "dest.Y <- src.X" create reverse "src.X <- dest.Y",
        // unless the reverse side configured that member itself.
        private static void BuildReverseMembers(TypeMapConfig reverse)
        {
            var forward = reverse.ReverseOf!;
            foreach (var fwd in forward.Members.Values)
            {
                if (fwd.Ignored || fwd.ValueConverterType != null || fwd.SimpleSourceMemberName == null)
                    continue;

                var reverseDestName = fwd.SimpleSourceMemberName;
                if (reverse.Members.ContainsKey(reverseDestName))
                    continue;

                var prop = reverse.SourceType.GetProperty(fwd.DestinationName, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || !prop.CanRead)
                    continue;

                reverse.Members[reverseDestName] = new MemberConfig
                {
                    DestinationName = reverseDestName,
                    SourceGetter = ReflectionCache.Getter(prop)
                };
            }
        }

        /// <summary>
        /// Throws if any destination property has no source (not configured, not ignored,
        /// no same-named source property). Call it once at startup in development.
        /// </summary>
        public void AssertConfigurationIsValid()
        {
            var errors = new List<string>();
            foreach (var map in TypeMaps.Values.Where(m => m.TypeConverter == null))
            {
                var missing = ReflectionCache.WritableProperties(map.DestinationType)
                    .Where(p => !map.Members.ContainsKey(p.Name)
                             && ReflectionCache.FindSourceProperty(map.SourceType, p.Name) == null)
                    .Select(p => p.Name)
                    .ToList();

                if (missing.Count > 0)
                    errors.Add($"{map.SourceType.Name} -> {map.DestinationType.Name}: unmapped {string.Join(", ", missing)}");
            }

            if (errors.Count > 0)
                throw new MiniMapperException("Unmapped members found:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }

        internal TypeMapConfig? FindTypeMap(Type source, Type destination)
        {
            for (var t = source; t != null; t = t.BaseType)
                if (TypeMaps.TryGetValue((t, destination), out var map))
                    return map;
            return null;
        }
    }

    // -----------------------------------------------------------------
    // Mapper (runtime engine)
    // -----------------------------------------------------------------

    public sealed class Mapper : IMapper
    {
        private delegate object? MapFunc(object source, object? destination, ResolutionContext ctx);

        // Plans depend only on configuration, so share them across all Mapper instances.
        private static readonly ConcurrentDictionary<(MapperConfiguration, Type, Type), MapFunc> PlanCache = new();

        private readonly MapperConfiguration _config;
        private readonly ResolutionContext _context;

        public Mapper(MapperConfiguration config, IServiceProvider? services = null)
        {
            _config = config;
            _context = new ResolutionContext(this, services);
        }

        public TDestination Map<TDestination>(object? source)
            => source == null ? default! : (TDestination)MapCore(source, typeof(TDestination), null)!;

        public TDestination Map<TSource, TDestination>(TSource source)
            => source == null ? default! : (TDestination)MapCore(source, typeof(TDestination), null)!;

        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
            => source == null ? destination : (TDestination)MapCore(source, typeof(TDestination), destination)!;

        public object? Map(object? source, Type sourceType, Type destinationType)
            => source == null ? null : MapCore(source, destinationType, null);

        private object? MapCore(object source, Type destinationType, object? destination)
            => MapCore(_config, source, destinationType, destination, _context);

        // Static on purpose: cached plans are shared by every Mapper instance (every DI scope),
        // so they must never capture a specific Mapper or IServiceProvider. Everything
        // scope-specific comes in through 'ctx' on each call.
        private static object? MapCore(MapperConfiguration config, object source, Type destinationType,
                                       object? destination, ResolutionContext ctx)
        {
            var plan = PlanCache.GetOrAdd((config, source.GetType(), destinationType),
                key => BuildPlan(key.Item1, key.Item2, key.Item3));
            try
            {
                return plan(source, destination, ctx);
            }
            catch (MiniMapperException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MiniMapperException($"Error mapping {source.GetType().Name} -> {destinationType.Name}: {ex.Message}", ex);
            }
        }

        // Converts a single value (member or root) to the target type.
        private static object? ConvertValue(MapperConfiguration config, object? value, Type destinationType, ResolutionContext ctx)
        {
            if (value == null)
            {
                if (IsCollection(destinationType))
                    return CreateEmptyCollection(destinationType);
                return ReflectionCache.DefaultValue(destinationType);
            }
            return MapCore(config, value, destinationType, null, ctx);
        }

        // ---------------- plan building ----------------

        private static MapFunc BuildPlan(MapperConfiguration config, Type src, Type dest)
        {
            // 1. Explicit type map wins (includes CreateMap<DateTime,string>().ConvertUsing(...)).
            var typeMap = config.FindTypeMap(src, dest);
            if (typeMap != null)
            {
                if (typeMap.TypeConverter != null)
                {
                    var conv = typeMap.TypeConverter;
                    return (s, _, _) => conv(s);
                }
                return BuildObjectPlan(config, typeMap);
            }

            // 2. Nullable<T> destination -> map to T.
            var underlying = Nullable.GetUnderlyingType(dest);
            if (underlying != null)
                return (s, _, ctx) => MapCore(config, s, underlying, null, ctx);

            // 3. Already compatible.
            if (dest.IsAssignableFrom(src) && !(IsCollection(dest) && IsCollection(src) && ElementType(src) != ElementType(dest)))
                return (s, _, _) => s;

            // 4. Strings.
            if (dest == typeof(string))
                return (s, _, _) => s is IFormattable f ? f.ToString(null, CultureInfo.CurrentCulture) : s.ToString();

            // 5. Enums.
            if (dest.IsEnum)
            {
                if (src == typeof(string))
                    return (s, _, _) => Enum.Parse(dest, (string)s, ignoreCase: true);
                return (s, _, _) => Enum.ToObject(dest, s);
            }
            if (src.IsEnum && IsNumeric(dest))
                return (s, _, _) => System.Convert.ChangeType(s, dest, CultureInfo.InvariantCulture);

            // 6. Collections.
            if (IsCollection(dest) && IsCollection(src))
                return BuildCollectionPlan(config, dest);

            // 7. Guid / primitives / DateTime etc.
            if (dest == typeof(Guid) && src == typeof(string))
                return (s, _, _) => Guid.Parse((string)s);

            if (typeof(IConvertible).IsAssignableFrom(src) && typeof(IConvertible).IsAssignableFrom(dest))
                return (s, _, _) =>
                {
                    if (s is string str && string.IsNullOrWhiteSpace(str))
                        return ReflectionCache.DefaultValue(dest);
                    return System.Convert.ChangeType(s, dest, CultureInfo.CurrentCulture);
                };

            var typeConverter = TypeDescriptor.GetConverter(dest);
            if (typeConverter.CanConvertFrom(src))
                return (s, _, _) => typeConverter.ConvertFrom(null, CultureInfo.CurrentCulture, s);

            // 8. Nothing fits.
            return (_, _, _) => throw new MiniMapperException(
                $"Missing map configuration: {src.FullName} -> {dest.FullName}. " +
                $"Add CreateMap<{src.Name}, {dest.Name}>() to a Profile.");
        }

        private static MapFunc BuildObjectPlan(MapperConfiguration config, TypeMapConfig map)
        {
            var factory = ReflectionCache.Factory(map.DestinationType);
            var steps = new List<Action<object, object, ResolutionContext>>();

            foreach (var destProp in ReflectionCache.WritableProperties(map.DestinationType))
            {
                var setter = ReflectionCache.Setter(destProp);
                var destPropType = destProp.PropertyType;
                map.Members.TryGetValue(destProp.Name, out var member);

                if (member?.Ignored == true)
                    continue;

                if (member?.ValueConverterType != null)
                {
                    var getter = member.SourceGetter!;
                    var converterType = member.ValueConverterType;
                    var invoke = member.ValueConverterInvoker!;
                    steps.Add((s, d, ctx) =>
                    {
                        var converter = ResolveService(converterType, ctx);
                        setter(d, invoke(converter, getter(s), ctx));
                    });
                    continue;
                }

                Func<object, object?>? sourceGetter = member?.SourceGetter;
                if (sourceGetter == null)
                {
                    var srcProp = ReflectionCache.FindSourceProperty(map.SourceType, destProp.Name);
                    if (srcProp == null)
                        continue; // unmapped: leave default value (AssertConfigurationIsValid reports it)
                    sourceGetter = ReflectionCache.Getter(srcProp);
                }

                var get = sourceGetter;
                steps.Add((s, d, ctx) => setter(d, ConvertValue(config, get(s), destPropType, ctx)));
            }

            var compiledSteps = steps.ToArray();
            return (s, d, ctx) =>
            {
                var target = d ?? factory();
                foreach (var step in compiledSteps)
                    step(s, target, ctx);
                return target;
            };
        }

        private static MapFunc BuildCollectionPlan(MapperConfiguration config, Type dest)
        {
            var elementType = ElementType(dest)!;
            var listType = typeof(List<>).MakeGenericType(elementType);
            var isArray = dest.IsArray;

            return (s, _, ctx) =>
            {
                var list = (IList)Activator.CreateInstance(listType)!;
                foreach (var item in (IEnumerable)s)
                    list.Add(ConvertValue(config, item, elementType, ctx));

                if (isArray)
                {
                    var array = Array.CreateInstance(elementType, list.Count);
                    list.CopyTo(array, 0);
                    return array;
                }
                if (dest.IsAssignableFrom(listType))
                    return list;

                // e.g. HashSet<T>, ObservableCollection<T>, Collection<T>
                return Activator.CreateInstance(dest, list);
            };
        }

        private static object ResolveService(Type type, ResolutionContext ctx)
        {
            if (ctx.Services != null)
                return ActivatorUtilities.GetServiceOrCreateInstance(ctx.Services, type);
            return Activator.CreateInstance(type)
                   ?? throw new MiniMapperException($"Cannot create {type.Name}: no IServiceProvider available.");
        }

        // ---------------- type helpers ----------------

        private static bool IsCollection(Type t)
            => t != typeof(string) && (t.IsArray || ElementType(t) != null);

        private static Type? ElementType(Type t)
        {
            if (t == typeof(string)) return null;
            if (t.IsArray) return t.GetElementType();
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return t.GetGenericArguments()[0];
            return t.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];
        }

        private static object? CreateEmptyCollection(Type t)
        {
            var element = ElementType(t)!;
            if (t.IsArray) return Array.CreateInstance(element, 0);
            var listType = typeof(List<>).MakeGenericType(element);
            if (t.IsAssignableFrom(listType)) return Activator.CreateInstance(listType);
            return t.IsAbstract || t.IsInterface ? null : Activator.CreateInstance(t);
        }

        private static bool IsNumeric(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }

    // -----------------------------------------------------------------
    // Reflection / expression helpers (all cached)
    // -----------------------------------------------------------------

    internal static class ReflectionCache
    {
        private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> Getters = new();
        private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> Setters = new();
        private static readonly ConcurrentDictionary<Type, Func<object>> Factories = new();
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Writable = new();
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> Readable = new();
        private static readonly ConcurrentDictionary<Type, object?> Defaults = new();

        public static PropertyInfo[] WritableProperties(Type t) => Writable.GetOrAdd(t, type =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.SetMethod!.IsPublic && p.GetIndexParameters().Length == 0)
                .ToArray());

        public static PropertyInfo? FindSourceProperty(Type t, string name)
        {
            var map = Readable.GetOrAdd(t, type =>
            {
                var dict = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    if (p.CanRead && p.GetIndexParameters().Length == 0)
                        dict.TryAdd(p.Name, p);
                return dict;
            });
            return map.TryGetValue(name, out var prop) ? prop : null;
        }

        public static Func<object, object?> Getter(PropertyInfo p) => Getters.GetOrAdd(p, prop =>
        {
            var obj = Expression.Parameter(typeof(object), "obj");
            var body = Expression.Convert(
                Expression.Property(Expression.Convert(obj, prop.DeclaringType!), prop),
                typeof(object));
            return Expression.Lambda<Func<object, object?>>(body, obj).Compile();
        });

        public static Action<object, object?> Setter(PropertyInfo p) => Setters.GetOrAdd(p, prop =>
        {
            var obj = Expression.Parameter(typeof(object), "obj");
            var value = Expression.Parameter(typeof(object), "value");
            var body = Expression.Assign(
                Expression.Property(Expression.Convert(obj, prop.DeclaringType!), prop),
                Expression.Convert(value, prop.PropertyType));
            return Expression.Lambda<Action<object, object?>>(body, obj, value).Compile();
        });

        public static Func<object> Factory(Type t) => Factories.GetOrAdd(t, type =>
        {
            if (type.GetConstructor(Type.EmptyTypes) == null)
                return () => throw new MiniMapperException($"{type.FullName} needs a public parameterless constructor to be mapped.");
            var body = Expression.Convert(Expression.New(type), typeof(object));
            return Expression.Lambda<Func<object>>(body).Compile();
        });

        public static object? DefaultValue(Type t)
            => t.IsValueType && Nullable.GetUnderlyingType(t) == null
                ? Defaults.GetOrAdd(t, Activator.CreateInstance)
                : null;
    }

    internal static class ExpressionHelper
    {
        public static string GetMemberName(LambdaExpression expression)
        {
            var body = expression.Body is UnaryExpression u ? u.Operand : expression.Body;
            if (body is MemberExpression m && m.Expression is ParameterExpression)
                return m.Member.Name;
            throw new MiniMapperException($"ForMember needs a direct property, e.g. dest => dest.Name. Got: {expression}");
        }

        public static string? TryGetSimpleMemberName(LambdaExpression expression)
        {
            var body = expression.Body is UnaryExpression u ? u.Operand : expression.Body;
            return body is MemberExpression m && m.Expression is ParameterExpression && m.Member is PropertyInfo
                ? m.Member.Name
                : null;
        }

        // src => src.A.B returns null instead of throwing when A is null (AutoMapper behaviour).
        public static Func<object, object?> CompileNullSafeGetter<TSource, TMember>(Expression<Func<TSource, TMember>> expression)
        {
            var compiled = expression.Compile();
            return src =>
            {
                try { return compiled((TSource)src); }
                catch (NullReferenceException) { return null; }
            };
        }
    }

    // -----------------------------------------------------------------
    // Dependency injection
    // -----------------------------------------------------------------

    public static class MiniMapperServiceCollectionExtensions
    {
        /// <summary>Scans the given assemblies for Profile classes.</summary>
        public static IServiceCollection AddMiniMapper(this IServiceCollection services, params Assembly[] assemblies)
        {
            var config = MapperConfiguration.FromAssemblies(assemblies);
            services.AddSingleton(config);
            // Scoped so value converters can take scoped dependencies.
            services.AddScoped<IMapper>(sp => new Mapper(sp.GetRequiredService<MapperConfiguration>(), sp));
            return services;
        }

        /// <summary>Scans the assemblies that contain the given marker types.</summary>
        public static IServiceCollection AddMiniMapper(this IServiceCollection services, params Type[] markerTypes)
            => services.AddMiniMapper(markerTypes.Select(t => t.Assembly).Distinct().ToArray());
    }
}