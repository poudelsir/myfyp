// SajhaSikshya — New Listing photo staging (Areas/Student/Views/Listings/Create.cshtml).
// The listing doesn't exist yet at this point, so there's no listing id to attach
// ListingImage rows to (that's what Edit's server-backed gallery — listing-media.js —
// needs and this doesn't have). Instead this just stages File objects client-side
// (preview + remove, no upload yet) and keeps the real <input type="file"> in sync via
// DataTransfer so the browser submits every staged file in one multipart POST together
// with the rest of the Create form. The server (ListingsController.Create) creates the
// listing first, then reuses the same UploadImagesAsync the Edit gallery already calls.
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        var input = document.getElementById("createImageInput");
        var dropzone = document.getElementById("createDropzone");
        var previewContainer = document.getElementById("stagedImagePreview");
        var requiredError = document.getElementById("imagesRequiredError");
        var form = document.getElementById("createListingForm");
        if (!input || !dropzone || !previewContainer) {
            return;
        }

        var maxImages = parseInt(input.dataset.maxImages, 10) || 8;
        var stagedFiles = [];
        var objectUrls = [];

        // The real <input> is visually hidden (the styled dropzone label is what the
        // seller actually interacts with), so the browser's native "required" validation
        // bubble would have nothing visible to anchor to and would silently block
        // submission with no explanation. Validate explicitly here instead, with a
        // visible message next to the dropzone.
        if (form) {
            form.addEventListener("submit", function (event) {
                if (stagedFiles.length === 0) {
                    event.preventDefault();
                    if (requiredError) {
                        requiredError.classList.remove("d-none");
                    }
                    dropzone.scrollIntoView({ behavior: "smooth", block: "center" });
                }
            });
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
            addFiles(event.dataTransfer.files);
        });

        input.addEventListener("change", function () {
            addFiles(input.files);
        });

        function addFiles(fileList) {
            Array.from(fileList || []).forEach(function (file) {
                if (stagedFiles.length >= maxImages || file.type.indexOf("image/") !== 0) {
                    return;
                }
                stagedFiles.push(file);
            });
            if (stagedFiles.length > 0 && requiredError) {
                requiredError.classList.add("d-none");
            }
            syncInputFiles();
            render();
        }

        function removeAt(index) {
            stagedFiles.splice(index, 1);
            syncInputFiles();
            render();
        }

        // Native FileList is read-only — DataTransfer is the standard way to replace a
        // file input's contents from script, so the real <input> always mirrors stagedFiles.
        function syncInputFiles() {
            var transfer = new DataTransfer();
            stagedFiles.forEach(function (file) { transfer.items.add(file); });
            input.files = transfer.files;
        }

        function render() {
            objectUrls.forEach(function (url) { URL.revokeObjectURL(url); });
            objectUrls = [];
            previewContainer.innerHTML = "";
            previewContainer.classList.toggle("d-none", stagedFiles.length === 0);
            dropzone.classList.toggle("d-none", stagedFiles.length >= maxImages);

            stagedFiles.forEach(function (file, index) {
                var url = URL.createObjectURL(file);
                objectUrls.push(url);

                var col = document.createElement("div");
                col.className = "col-6 col-md-3";

                var card = document.createElement("div");
                card.className = "marketplace-image-card" + (index === 0 ? " is-thumbnail" : "");

                var img = document.createElement("img");
                img.src = url;
                img.alt = "Selected photo";
                card.appendChild(img);

                if (index === 0) {
                    var badge = document.createElement("span");
                    badge.className = "badge marketplace-thumbnail-badge";
                    badge.textContent = "Cover";
                    card.appendChild(badge);
                }

                var actions = document.createElement("div");
                actions.className = "marketplace-image-actions";

                var removeBtn = document.createElement("button");
                removeBtn.type = "button";
                removeBtn.className = "btn btn-sm btn-outline-light";
                removeBtn.title = "Remove";
                removeBtn.setAttribute("aria-label", "Remove photo");
                removeBtn.innerHTML = '<i data-lucide="x"></i>';
                removeBtn.addEventListener("click", function () { removeAt(index); });
                actions.appendChild(removeBtn);

                card.appendChild(actions);
                col.appendChild(card);
                previewContainer.appendChild(col);
            });

            if (window.lucide) {
                window.lucide.createIcons();
            }
        }
    });
})();
