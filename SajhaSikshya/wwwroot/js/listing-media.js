// SajhaSikshya — Listing media management.
// Loaded only on the Listing Edit page. Handles drag-and-drop file selection for
// uploads, drag-and-drop reordering of already-uploaded photos (persisted via a
// background fetch()), and the photo preview modal. The server is always the
// authority — reordering just re-numbers DisplayOrder to match what's already
// visible on screen, and a failed save simply reverts to the saved order on
// next page load rather than needing complex client-side error recovery.

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        initDropzone();
        initReordering();
        initPreviewModal();
        initReplace();
    });

    function initReplace() {
        // Each image tile's "Replace" control is a label wrapping a hidden file input;
        // choosing a file submits that image's own form immediately, so replacing a
        // photo is a single click + file pick rather than a separate confirm step.
        document.querySelectorAll(".js-replace-input").forEach(function (input) {
            input.addEventListener("change", function () {
                if (input.files.length > 0) {
                    input.form.submit();
                }
            });
        });
    }

    function initDropzone() {
        var dropzone = document.getElementById("dropzone");
        var fileInput = document.getElementById("fileInput");
        var fileList = document.getElementById("fileList");
        var uploadBtn = document.getElementById("uploadBtn");
        var uploadForm = document.getElementById("uploadForm");
        var uploadSpinner = document.getElementById("uploadSpinner");

        if (!dropzone || !fileInput) {
            return;
        }

        dropzone.addEventListener("dragover", function (event) {
            event.preventDefault();
            dropzone.classList.add("is-dragover");
        });

        dropzone.addEventListener("dragleave", function () {
            dropzone.classList.remove("is-dragover");
        });

        dropzone.addEventListener("drop", function (event) {
            event.preventDefault();
            dropzone.classList.remove("is-dragover");
            if (event.dataTransfer.files.length > 0) {
                fileInput.files = event.dataTransfer.files;
                renderFileList();
            }
        });

        fileInput.addEventListener("change", renderFileList);

        function renderFileList() {
            var files = Array.from(fileInput.files || []);
            fileList.innerHTML = "";

            files.forEach(function (file) {
                var item = document.createElement("div");
                item.textContent = file.name + " (" + (file.size / 1024 / 1024).toFixed(2) + " MB)";
                fileList.appendChild(item);
            });

            uploadBtn.disabled = files.length === 0;
        }

        uploadForm.addEventListener("submit", function () {
            uploadBtn.disabled = true;
            uploadSpinner?.classList.remove("d-none");
        });
    }

    function initReordering() {
        var gallery = document.getElementById("imageGallery");
        if (!gallery) {
            return;
        }

        var draggedItem = null;

        gallery.querySelectorAll(".marketplace-image-item").forEach(function (item) {
            item.addEventListener("dragstart", function () {
                draggedItem = item;
                item.classList.add("is-dragging");
            });

            item.addEventListener("dragend", function () {
                item.classList.remove("is-dragging");
                draggedItem = null;
                persistOrder();
            });

            item.addEventListener("dragover", function (event) {
                event.preventDefault();
                if (!draggedItem || draggedItem === item) {
                    return;
                }
                var rect = item.getBoundingClientRect();
                var isBeforeMidpoint = (event.clientX - rect.left) < rect.width / 2;
                gallery.insertBefore(draggedItem, isBeforeMidpoint ? item : item.nextSibling);
            });
        });

        function persistOrder() {
            var orderedIds = Array.from(gallery.querySelectorAll(".marketplace-image-item"))
                .map(function (el) { return el.dataset.imageId; });

            var token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            var body = new URLSearchParams();
            body.append("listingId", gallery.dataset.listingId);
            orderedIds.forEach(function (id) { body.append("orderedImageIds", id); });
            if (token) {
                body.append("__RequestVerificationToken", token);
            }

            fetch(gallery.dataset.reorderUrl, {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: body.toString(),
            }).catch(function () {
                // Non-fatal — see the file header comment.
            });
        }
    }

    function initPreviewModal() {
        var modalImage = document.getElementById("previewModalImage");
        var previewModalEl = document.getElementById("previewModal");
        if (!modalImage || !previewModalEl || !window.bootstrap) {
            return;
        }

        var modal = new window.bootstrap.Modal(previewModalEl);

        document.querySelectorAll(".js-preview-image").forEach(function (button) {
            button.addEventListener("click", function () {
                modalImage.src = button.dataset.imageSrc || "";
                modal.show();
            });
        });
    }
})();
