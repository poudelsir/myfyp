// SajhaSikshya — real-time Notification Center (Phase 8.1).
//
// One connection per authenticated page, mirroring chat.js's shape exactly: the Hub
// itself gates who may connect ([Authorize], any authenticated role — Students and
// Admins alike, since notifications are equally private to both), so this simply
// attempts a connection for every authenticated user and lets a rejected connection
// fail silently (logged, not surfaced). Fully replaces the original placeholder now
// that a real notification-producing feature (Chat) exists.

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        const isAuthenticated = document.body.dataset.authenticated === "true";
        if (!isAuthenticated || typeof signalR === "undefined") {
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/notifications")
            .withAutomaticReconnect()
            .build();

        const dot = document.getElementById("notificationDot");
        const unreadBadge = document.getElementById("notificationUnreadBadge");
        const panelList = document.getElementById("notificationPanelList");
        const panelEmpty = document.getElementById("notificationPanelEmpty");
        const markAllBtn = document.getElementById("notificationMarkAllReadBtn");

        function getAntiForgeryToken() {
            const input = document.querySelector('#notificationTokenForm input[name="__RequestVerificationToken"]');
            return input ? input.value : "";
        }

        function postForm(url) {
            const body = new URLSearchParams();
            body.set("__RequestVerificationToken", getAntiForgeryToken());
            return fetch(url, { method: "POST", body: body, credentials: "same-origin" });
        }

        function updateUnreadUi(count) {
            if (unreadBadge) {
                unreadBadge.textContent = String(count);
                unreadBadge.classList.toggle("d-none", count <= 0);
            }

            dot?.classList.toggle("d-none", count <= 0);
            markAllBtn?.classList.toggle("d-none", count <= 0);
        }

        function iconFor(notificationType) {
            switch (notificationType) {
                case 0: return "message-circle"; // Message
                case 1: return "package"; // Order
                case 2: return "star"; // Review
                case 3: return "shield-check"; // Verification
                case 5: return "sparkles"; // AIRecommendation
                case 6: return "megaphone"; // Announcement
                default: return "bell";
            }
        }

        function wireItemClick(item) {
            item.addEventListener("click", function () {
                const id = item.dataset.notificationId;
                if (id && item.classList.contains("is-unread")) {
                    item.classList.remove("is-unread");
                    postForm("/notifications/" + id + "/mark-read").catch(function () { });
                }
            });
        }

        function prependNotification(notification) {
            if (!panelList) {
                return;
            }

            panelEmpty?.classList.add("d-none");
            panelList.classList.remove("d-none");

            const link = document.createElement("a");
            link.className = "notification-panel-item is-unread";
            link.href = notification.link || "/notifications";
            link.dataset.notificationId = String(notification.id);

            const icon = document.createElement("i");
            icon.setAttribute("data-lucide", iconFor(notification.notificationType));
            link.appendChild(icon);

            const body = document.createElement("span");
            body.className = "notification-panel-item-body";

            const titleEl = document.createElement("span");
            titleEl.className = "notification-panel-item-title";
            titleEl.textContent = notification.title;

            const msgEl = document.createElement("span");
            msgEl.className = "notification-panel-item-message";
            msgEl.textContent = notification.message;

            body.appendChild(titleEl);
            body.appendChild(msgEl);
            link.appendChild(body);

            wireItemClick(link);
            panelList.insertBefore(link, panelList.firstChild);

            // The dropdown only ever shows a handful of "recent" items — the full
            // history lives on the Notification Center page — so trim from the bottom
            // whenever a live arrival pushes past that cap.
            while (panelList.children.length > 5) {
                panelList.removeChild(panelList.lastChild);
            }

            window.lucide?.createIcons();
        }

        panelList?.querySelectorAll(".notification-panel-item").forEach(wireItemClick);

        markAllBtn?.addEventListener("click", function () {
            postForm("/notifications/mark-all-read")
                .then(function () {
                    panelList?.querySelectorAll(".notification-panel-item.is-unread").forEach(function (el) {
                        el.classList.remove("is-unread");
                    });
                })
                .catch(function (err) {
                    console.error("Mark all read failed:", err);
                });
        });

        connection.on("ReceiveNotification", function (notification) {
            prependNotification(notification);
        });

        connection.on("UnreadCountUpdated", function (count) {
            updateUnreadUi(count);
        });

        connection.on("NotificationRead", function (notificationId) {
            panelList?.querySelector('[data-notification-id="' + notificationId + '"]')?.classList.remove("is-unread");
        });

        connection.on("NotificationDeleted", function (notificationId) {
            const item = panelList?.querySelector('[data-notification-id="' + notificationId + '"]');
            item?.remove();

            if (panelList && panelList.children.length === 0) {
                panelList.classList.add("d-none");
                panelEmpty?.classList.remove("d-none");
            }
        });

        connection.on("AllNotificationsRead", function () {
            panelList?.querySelectorAll(".notification-panel-item.is-unread").forEach(function (el) {
                el.classList.remove("is-unread");
            });
        });

        connection.start().catch(function (err) {
            console.error("Notification hub connection failed:", err);
        });
    });
})();
