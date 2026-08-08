// Student Dashboard (Areas/Student/Views/Dashboard/Index.cshtml). Both charts render
// immediately from data the server already embedded — no fetch(), no loading state.
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        renderMyListingStatusChart();
        renderMyOrdersChart();
    });

    function renderMyListingStatusChart() {
        var canvas = document.getElementById("myListingStatusChart");
        var empty = document.getElementById("myListingStatusEmpty");
        var stats = readJson("myListingStatusData");

        if (!window.Chart || !canvas || !stats || stats.total === 0) {
            toggleEmpty(canvas, empty);
            return;
        }

        var labels = ["Draft", "Pending", "Active", "Reserved", "Sold", "Donated", "Archived", "Rejected"];
        var data = [stats.draft, stats.pendingApproval, stats.active, stats.reserved, stats.sold, stats.donated, stats.archived, stats.rejected];
        var colors = ["#6c757d", "#0dcaf0", "#198754", "#ffc107", "#0d6efd", "#fd7e14", "#adb5bd", "#dc3545"];

        new window.Chart(canvas, {
            type: "doughnut",
            data: {
                labels: labels,
                datasets: [{ data: data, backgroundColor: colors }],
            },
            options: {
                responsive: true,
                plugins: { legend: { position: "bottom" } },
            },
        });
    }

    function renderMyOrdersChart() {
        var canvas = document.getElementById("myOrdersChart");
        var empty = document.getElementById("myOrdersEmpty");
        var stats = readJson("myOrdersData");

        var hasData = stats && stats.buyer && stats.seller && (stats.buyer.totalCount > 0 || stats.seller.totalCount > 0);
        if (!window.Chart || !canvas || !hasData) {
            toggleEmpty(canvas, empty);
            return;
        }

        var labels = ["Pending", "Confirmed", "Ready for Pickup", "Completed", "Cancelled"];

        new window.Chart(canvas, {
            type: "bar",
            data: {
                labels: labels,
                datasets: [
                    {
                        label: "As Buyer",
                        data: [stats.buyer.pendingCount, stats.buyer.confirmedCount, stats.buyer.readyForPickupCount, stats.buyer.completedCount, stats.buyer.cancelledCount],
                        backgroundColor: "#0d6efd",
                        borderRadius: 4,
                    },
                    {
                        label: "As Seller",
                        data: [stats.seller.pendingCount, stats.seller.confirmedCount, stats.seller.readyForPickupCount, stats.seller.completedCount, stats.seller.cancelledCount],
                        backgroundColor: "#198754",
                        borderRadius: 4,
                    },
                ],
            },
            options: {
                responsive: true,
                plugins: { legend: { position: "bottom" } },
                scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
            },
        });
    }

    function toggleEmpty(canvas, empty) {
        if (canvas) {
            canvas.classList.add("d-none");
        }
        if (empty) {
            empty.classList.remove("d-none");
        }
    }

    function readJson(elementId) {
        var el = document.getElementById(elementId);
        if (!el) {
            return null;
        }

        try {
            return JSON.parse(el.textContent);
        } catch (e) {
            return null;
        }
    }
})();
