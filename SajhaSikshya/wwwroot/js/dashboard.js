// SajhaSikshya — dashboard shell interactions.
// Handles the responsive sidebar: an off-canvas drawer on small screens
// (toggled + closed via backdrop/close button) and a collapsible icon rail
// on large screens, persisted across page loads.

(function () {
    "use strict";

    const COLLAPSE_STORAGE_KEY = "sajhasikshya-sidebar-collapsed";

    document.addEventListener("DOMContentLoaded", function () {
        const shell = document.querySelector(".app-shell");
        if (!shell) {
            return;
        }

        const toggleBtn = document.getElementById("sidebarToggleBtn");
        const closeBtn = document.getElementById("sidebarCloseBtn");
        const backdrop = document.getElementById("sidebarBackdrop");

        const isDesktop = () => window.matchMedia("(min-width: 992px)").matches;

        if (isDesktop() && localStorage.getItem(COLLAPSE_STORAGE_KEY) === "true") {
            shell.classList.add("sidebar-collapsed");
        }

        function openMobileSidebar() {
            shell.classList.add("sidebar-open");
        }

        function closeMobileSidebar() {
            shell.classList.remove("sidebar-open");
        }

        toggleBtn?.addEventListener("click", function () {
            if (isDesktop()) {
                shell.classList.toggle("sidebar-collapsed");
                localStorage.setItem(COLLAPSE_STORAGE_KEY, shell.classList.contains("sidebar-collapsed"));
            } else {
                openMobileSidebar();
            }
        });

        closeBtn?.addEventListener("click", closeMobileSidebar);
        backdrop?.addEventListener("click", closeMobileSidebar);

        window.addEventListener("resize", function () {
            if (isDesktop()) {
                closeMobileSidebar();
            }
        });
    });
})();
