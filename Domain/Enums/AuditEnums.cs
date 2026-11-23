// ================================================================
// Domain / Enums / AuditEnums.cs
// All domain enumerations derived from DB seed data
// ================================================================
namespace Domain.Enums;

/// <summary>Maps to MASTER_AUDIT_STATUS.STATUS_ID seed data</summary>
public enum AuditStatusEnum
{
    Draft = 1,
    InProgress = 2,
    Submitted = 3,
    UnderReview = 4,
    Approved = 5,
    Rejected = 6
}

/// <summary>Maps to MASTER_AUDIT_GRADE.GRADE_CODE</summary>
public enum AuditGradeCode
{
    A = 1,  // 81–100 % — 3 Year Validation — Green
    B = 2,  // 51–80 % — 2 Year Validation — Yellow
    C = 3,  // 31–50 % — 1 Year Validation — Orange
    D = 4   //  0–30 % — 6 Month / Blacklist — Red
}

/// <summary>Maps to MASTER_AUDIT_RATING.SCORE</summary>
public enum CheckPointScore
{
    NotApplicable = 0,
    Poor = 2,
    BelowAverage = 4,
    Average = 6,
    Good = 8,
    Excellent = 10
}

/// <summary>Document availability flag</summary>
public enum DocumentAvailability
{
    NotSet = 0,
    Yes = 1,
    No = 2
}

/// <summary>Wizard step identifiers — each maps to one UI tab/step</summary>
public enum WizardStep
{
    AuditorInfo = 1,
    VendorInfo = 2,
    DocumentChecklist = 3,
    RawMaterials = 4,   // Element 1
    ProductDevelopment = 5,   // Element 2
    Cutting = 6,   // Element 3
    Sewing = 7,   // Element 4
    Finishing = 8,   // Element 5
    Packing = 9,   // Element 6
    LaboratoryTesting = 10,  // Element 7
    QAManagement = 11,  // Element 8
    QCSystem = 12,  // Element 9
    MinimumCompliances = 13,  // Element 10
    PhotoAttachments = 14,
    OverallComments = 15,
    ReviewAndSubmit = 16
}
