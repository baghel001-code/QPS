

function disableRightClickAndRemoveDownload() {
    // Select all video elements on the page
    const videoElements = document.querySelectorAll('video');
    // Loop through each video element and attach an event listener to disable right-click
    videoElements.forEach(video => {
        video.addEventListener('contextmenu', function (e) {
            e.preventDefault(); // This will disable the right-click menu entirely for video elements
        });
        video.setAttribute("controlslist", "nodownload");
    });
}

function AddTable() {
    // Ensure that Quill is loaded
    const quill = document.querySelector('.ql-editor');
    if (quill) {
        // Register quill-better-table
        const BetterTable = window.QuillBetterTable.default;
        if (BetterTable) {
            quill.quill.addModule('better-table', BetterTable);
        }
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Select all input elements
    var inputs = document.querySelectorAll('h1');

    // Loop through each input and remove the 'tabindex' attribute
    inputs.forEach(function (h1) {
        h1.removeAttribute('tabindex');
    });
});

//window.addEventListener("offline", () => {
//    document.getElementById("components-reconnect-modal").style.display = "none";
//});

window.getScrollTop = (element) => element.scrollTop;
window.getScrollHeight = (element) => element.scrollHeight;
window.getClientHeight = (element) => element.clientHeight;


window.downloadBase64Pdf = function (base64, filename) {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], {type: "application/pdf" });

    const link = document.createElement("a");
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    };

window.downloadFileFromBase64 = (filename, mimeType, base64) => {
    const link = document.createElement('a');
    link.href = "data:" + mimeType + ";base64," + base64;
    link.download = filename;
    link.click();
};

window.startQRScanner = async function () {
    try {
        const constraints = { video: { facingMode: "environment" } };
        const stream = await navigator.mediaDevices.getUserMedia(constraints);

        const video = document.getElementById("qr-video");
        video.style.display = "block";
        video.srcObject = stream;

        const scanner = new ZXing.BrowserQRCodeReader();
        scanner.decodeFromVideoDevice(null, "qr-video", (result, err) => {
            if (result) {
                // Stop stream after successful scan
                stream.getTracks().forEach(track => track.stop());
                video.style.display = "none";
                DotNet.invokeMethodAsync('SetScannedValue', result.text);
                scanner.reset();
            }
        });
    } catch (err) {
        alert("Camera access denied or not available.");
        console.error(err);
    }
}