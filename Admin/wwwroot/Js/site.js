function downloadFile(fileName, base64Data) {
    const link = document.createElement('a');
    link.download = fileName;
    link.href = "data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64," + base64Data;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

window.printReport = function () {
    const printContents = document.getElementById('print-area').innerHTML;
    const originalContents = document.body.innerHTML;

    document.body.innerHTML = printContents;
    window.print();
    document.body.innerHTML = originalContents;
    location.reload();
}

window.openFileInNewTab = (bytes, contentType) => {
    const blob = new Blob([new Uint8Array(bytes)], { type: contentType });
    const url = URL.createObjectURL(blob);
    window.open(url, '_blank');
};

window.printAbortiveReport = function () {
    const printContents = document.getElementById('print-area').innerHTML;

    const printWindow = window.open('', '_blank');

    printWindow.document.open();
    printWindow.document.write(`
        <html>
            <head>
                <title>Print</title>
               
                 <!-- Your app styles -->
                <link href="Bootstrap/bootstrap.min.css" rel="stylesheet" crossorigin="anonymous">
                <link href="Bootstrap/font/bootstrap-icons.min.css" rel="stylesheet" />
                <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
                <link href="resources/css/quill.snow.css" rel="stylesheet" />
                <link href="resources/css/vmm.css?v=1.0.4" rel="stylesheet" />

                <style>
                    @media print {
                        body { margin: 20px;  font-family: Arial, sans-serif; padding: 20px; }
                    }
                </style>
            </head>
            <body>
                ${printContents}
            </body>
        </html>
    `);
    printWindow.document.close();

    printWindow.focus();
    printWindow.print();

    // Optional: auto close after print
    printWindow.onafterprint = function () {
        printWindow.close();
    };
};

// Opens in a new tab (no forced download)
window.openPOPdfInNewTab = (base64Data, filename) => {
    const bytes = Uint8Array.from(atob(base64Data), c => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: 'application/pdf' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.target = '_blank';      // new tab
    // link.download = filename; // uncomment to force download instead
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 10000); // cleanup blob URL
};


