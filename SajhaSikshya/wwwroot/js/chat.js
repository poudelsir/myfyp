// SajhaSikshya — real-time Chat (Phase 7.2).
//
// One connection per page, established for every authenticated page (the Hub itself
// enforces [Authorize(Roles = Student)] — a non-Student's connection attempt simply
// fails silently here, exactly how notifications.js already treats connection
// failures). The connection always listens for "UnreadCountUpdated" to keep the
// sidebar badge live; if the page also has a #chatContainer element (only the
// Conversation view renders one), it additionally joins that conversation's group and
// wires up live message delivery, typing indicators, read receipts, and progressive
// enhancement of the Send/Edit/Delete forms already built in Phase 7.1 (they keep
// working as plain HTTP POSTs if SignalR never connects — this only intercepts them
// once a connection is live).
const MESSAGE_TYPE_TEXT = 0;
const MESSAGE_TYPE_IMAGE = 1;
const MESSAGE_TYPE_FILE = 2;

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        const isAuthenticated = document.body.dataset.authenticated === "true";
        if (!isAuthenticated || typeof signalR === "undefined") {
            return;
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/chat")
            .withAutomaticReconnect()
            .build();

        wireUnreadBadge(connection);

        const chatContainer = document.getElementById("chatContainer");
        const afterStart = chatContainer ? wireConversation(connection, chatContainer) : null;

        connection.start()
            .then(function () {
                if (afterStart) {
                    afterStart();
                }
            })
            .catch(function (err) {
                console.error("Chat hub connection failed:", err);
            });
    });

    function wireUnreadBadge(connection) {
        connection.on("UnreadCountUpdated", function (count) {
            const badge = document.getElementById("chatUnreadBadge");
            if (!badge) {
                return;
            }

            badge.textContent = String(count);
            badge.classList.toggle("d-none", count <= 0);
        });
    }

    function wireConversation(connection, container) {
        // Referenced by dynamically-appended bubbles' Edit/Delete handlers (see
        // appendOrReplaceBubble/startInlineEdit) — those elements don't exist at
        // DOMContentLoaded, so they can't close over `connection` the way the
        // server-rendered forms wired in wireEditForms/wireDeleteForms do.
        window.chatConnection = connection;

        const conversationId = parseInt(container.dataset.conversationId, 10);
        const currentUserId = container.dataset.currentUserId;
        const otherPartyName = container.dataset.otherPartyName;

        const statusEl = document.getElementById("chatConnectionStatus");
        const messagesEl = document.getElementById("chatMessages");
        const typingEl = document.getElementById("typingIndicator");
        const sendForm = document.getElementById("chatSendForm");
        const messageInput = document.getElementById("chatMessageText");

        function joinConversation() {
            connection.invoke("JoinConversation", conversationId).catch(function (err) {
                console.error("Failed to join conversation:", err);
            });
        }

        connection.on("ReceiveMessage", function (message) {
            if (message.conversationId !== conversationId) {
                return;
            }

            const emptyState = document.getElementById("chatEmptyState");
            emptyState?.remove();

            appendOrReplaceBubble(messagesEl, message, currentUserId, conversationId);
            messagesEl.scrollTop = messagesEl.scrollHeight;

            if (message.senderId !== currentUserId) {
                connection.invoke("MarkAsRead", conversationId).catch(function (err) {
                    console.error("Failed to mark as read:", err);
                });
            }
        });

        connection.on("MessageEdited", function (convId, messageId, newText, editedAtUtc) {
            if (convId !== conversationId) {
                return;
            }

            const row = messagesEl.querySelector('[data-message-id="' + messageId + '"]');
            const textEl = row?.querySelector(".chat-message-text");
            const markerEl = row?.querySelector(".chat-edited-marker");
            if (textEl) {
                textEl.textContent = newText;
            }
            markerEl?.classList.remove("d-none");
        });

        connection.on("MessageDeleted", function (convId, messageId) {
            if (convId !== conversationId) {
                return;
            }

            const row = messagesEl.querySelector('[data-message-id="' + messageId + '"]');
            const body = row?.querySelector(".chat-message-body");
            if (body) {
                body.innerHTML = "";
                const placeholder = document.createElement("p");
                placeholder.className = "mb-0 fst-italic small chat-message-deleted-text";
                placeholder.style.opacity = "0.75";
                placeholder.textContent = "This message was deleted.";
                body.appendChild(placeholder);
            }
        });

        connection.on("TypingStarted", function (convId, userId, userName) {
            if (convId !== conversationId || userId === currentUserId || !typingEl) {
                return;
            }

            typingEl.textContent = (userName || otherPartyName) + " is typing…";
            typingEl.classList.remove("d-none");
        });

        connection.on("TypingStopped", function (convId, userId) {
            if (convId !== conversationId || userId === currentUserId || !typingEl) {
                return;
            }

            typingEl.classList.add("d-none");
        });

        connection.on("ReadReceiptUpdated", function (convId) {
            if (convId !== conversationId) {
                return;
            }

            messagesEl.querySelectorAll('.chat-bubble-mine .chat-read-status').forEach(function (el) {
                el.textContent = "Read";
            });
        });

        connection.onreconnecting(function () {
            if (statusEl) {
                statusEl.textContent = "Reconnecting…";
                statusEl.classList.remove("d-none");
            }
        });

        connection.onreconnected(function () {
            if (statusEl) {
                statusEl.classList.add("d-none");
            }

            // Group membership is per-connection; a reconnect gets a new connection id,
            // so the group has to be re-joined.
            joinConversation();
        });

        connection.onclose(function () {
            if (statusEl) {
                statusEl.textContent = "Connection lost. Messages will resume once you reload.";
                statusEl.classList.remove("d-none");
            }
        });

        window.addEventListener("beforeunload", function () {
            connection.invoke("LeaveConversation", conversationId).catch(function () {
                // Best-effort — the connection is closing anyway.
            });
        });

        wireSendForm(connection, sendForm, messageInput, conversationId);
        wireTypingIndicatorTrigger(connection, messageInput, conversationId);
        wireEditForms(connection, container, conversationId);
        wireDeleteForms(connection, container, conversationId);
        wireAttachmentUpload(container);
        wireImagePreviewModal(container);

        return joinConversation;
    }

    function wireAttachmentUpload(container) {
        const form = document.getElementById("chatAttachmentForm");
        const input = document.getElementById("chatAttachmentInput");
        const statusLabel = document.getElementById("chatAttachmentFileName");
        if (!form || !input) {
            return;
        }

        input.addEventListener("change", function () {
            if (!input.files || input.files.length === 0) {
                return;
            }

            const file = input.files[0];
            if (statusLabel) {
                statusLabel.textContent = "Uploading " + file.name + "…";
            }

            const formData = new FormData(form);
            fetch(form.action, { method: "POST", body: formData, credentials: "same-origin" })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error("Upload failed with status " + response.status);
                    }

                    if (statusLabel) {
                        statusLabel.textContent = "";
                    }

                    input.value = "";
                    // No manual DOM update needed — ChatService dispatches the new
                    // attachment message through the same SignalR broadcast a text
                    // message uses, so it arrives via the ReceiveMessage handler above
                    // for every open connection, including this one.
                })
                .catch(function (err) {
                    console.error("Attachment upload failed:", err);
                    if (statusLabel) {
                        statusLabel.textContent = "Upload failed. Please try again.";
                    }
                });
        });
    }

    function wireImagePreviewModal(container) {
        container.addEventListener("click", function (event) {
            const link = event.target.closest(".chat-attachment-image-link");
            if (!link) {
                return;
            }

            event.preventDefault();
            const img = document.getElementById("chatImagePreviewImg");
            const title = document.getElementById("chatImagePreviewTitle");
            const modalEl = document.getElementById("chatImagePreviewModal");
            if (!img || !modalEl || !window.bootstrap) {
                return;
            }

            img.src = link.dataset.fullSrc;
            if (title) {
                title.textContent = link.dataset.fileName || "Preview";
            }

            window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
        });
    }

    function wireSendForm(connection, form, input, conversationId) {
        if (!form || !input) {
            return;
        }

        form.addEventListener("submit", function (event) {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                return; // let the plain HTTP POST fall back through.
            }

            event.preventDefault();
            const text = input.value.trim();
            if (!text) {
                return;
            }

            connection.invoke("SendMessage", conversationId, text)
                .then(function () {
                    input.value = "";
                    connection.invoke("StopTyping", conversationId).catch(function () { });
                })
                .catch(function (err) {
                    console.error("Failed to send message:", err);
                });
        });
    }

    function wireTypingIndicatorTrigger(connection, input, conversationId) {
        if (!input) {
            return;
        }

        let isTyping = false;
        let stopTimeout = null;

        input.addEventListener("input", function () {
            if (connection.state !== signalR.HubConnectionState.Connected) {
                return;
            }

            if (!isTyping) {
                isTyping = true;
                connection.invoke("StartTyping", conversationId).catch(function () { });
            }

            clearTimeout(stopTimeout);
            stopTimeout = setTimeout(function () {
                isTyping = false;
                connection.invoke("StopTyping", conversationId).catch(function () { });
            }, 2000);
        });
    }

    function wireEditForms(connection, container, conversationId) {
        container.querySelectorAll(".chat-edit-form").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                if (connection.state !== signalR.HubConnectionState.Connected) {
                    return;
                }

                event.preventDefault();
                const messageId = parseInt(form.dataset.messageId, 10);
                const input = form.querySelector('input[name="text"]');
                const text = input.value.trim();
                if (!text) {
                    return;
                }

                connection.invoke("EditMessage", conversationId, messageId, text)
                    .then(function () {
                        const collapseEl = form.closest(".collapse");
                        if (collapseEl && window.bootstrap) {
                            window.bootstrap.Collapse.getOrCreateInstance(collapseEl).hide();
                        }
                    })
                    .catch(function (err) {
                        console.error("Failed to edit message:", err);
                    });
            });
        });
    }

    function wireDeleteForms(connection, container, conversationId) {
        container.querySelectorAll(".chat-delete-form").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                // site.js's capture-phase confirm handler runs first; if the user
                // cancelled, the event is already prevented by the time we get here.
                if (event.defaultPrevented || connection.state !== signalR.HubConnectionState.Connected) {
                    return;
                }

                event.preventDefault();
                const messageId = parseInt(form.dataset.messageId, 10);
                connection.invoke("DeleteMessage", conversationId, messageId).catch(function (err) {
                    console.error("Failed to delete message:", err);
                });
            });
        });
    }

    function appendOrReplaceBubble(messagesEl, message, currentUserId, conversationId) {
        const existing = messagesEl.querySelector('[data-message-id="' + message.id + '"]');
        existing?.remove();

        const isMine = message.senderId === currentUserId;

        const row = document.createElement("div");
        row.className = "chat-bubble-row" + (isMine ? " is-mine" : "");
        row.dataset.messageId = String(message.id);

        const bubble = document.createElement("div");
        bubble.className = "chat-bubble " + (isMine ? "chat-bubble-mine" : "chat-bubble-theirs");
        bubble.dataset.senderId = message.senderId;

        const body = document.createElement("div");
        body.className = "chat-message-body";
        body.appendChild(buildMessageContent(message));

        const metaRow = document.createElement("div");
        metaRow.className = "d-flex align-items-center gap-2 flex-wrap";

        const metaSpan = document.createElement("span");
        metaSpan.className = "chat-bubble-meta chat-message-meta";
        metaSpan.textContent = formatTime(message.createdAtUtc);

        const editedMarker = document.createElement("span");
        editedMarker.className = "chat-edited-marker d-none";
        editedMarker.textContent = " · edited";
        metaSpan.appendChild(editedMarker);

        if (isMine) {
            metaSpan.appendChild(document.createTextNode(" · "));
            const readStatus = document.createElement("span");
            readStatus.className = "chat-read-status";
            readStatus.textContent = message.readAtUtc ? "Read" : "Sent";
            metaSpan.appendChild(readStatus);
        }

        metaRow.appendChild(metaSpan);

        if (isMine && message.messageType === MESSAGE_TYPE_TEXT) {
            const editBtn = document.createElement("button");
            editBtn.type = "button";
            editBtn.className = "btn btn-link btn-sm p-0 chat-bubble-meta";
            editBtn.textContent = "Edit";
            editBtn.addEventListener("click", function () {
                startInlineEdit(bubble, message, currentUserId, conversationId);
            });
            metaRow.appendChild(editBtn);
        }

        if (isMine) {
            const deleteBtn = document.createElement("button");
            deleteBtn.type = "button";
            deleteBtn.className = "btn btn-link btn-sm p-0 chat-bubble-meta";
            deleteBtn.textContent = "Delete";
            deleteBtn.addEventListener("click", function () {
                if (window.confirm("Delete this message?")) {
                    window.chatConnection?.invoke("DeleteMessage", conversationId, message.id).catch(function () { });
                }
            });
            metaRow.appendChild(deleteBtn);
        }

        body.appendChild(metaRow);
        bubble.appendChild(body);
        row.appendChild(bubble);
        messagesEl.appendChild(row);
    }

    /// Builds the type-specific content node for a live-received message — mirrors
    /// Conversation.cshtml's server-rendered markup exactly (same classes/structure) so
    /// live-appended and page-loaded bubbles look and behave identically. Only
    /// `textContent`/attribute assignment is used for anything derived from user input
    /// (message text, filenames) — never `innerHTML` — so a message can never inject
    /// markup into another participant's page.
    function buildMessageContent(message) {
        if (message.messageType === MESSAGE_TYPE_IMAGE) {
            const link = document.createElement("a");
            link.href = "#";
            link.className = "chat-attachment-image-link";
            link.dataset.fullSrc = attachmentUrl(message.id, false);
            link.dataset.fileName = message.originalFileName || "";

            const img = document.createElement("img");
            img.src = attachmentUrl(message.id, false);
            img.alt = message.originalFileName || "";
            img.className = "chat-attachment-thumb";
            link.appendChild(img);
            return link;
        }

        if (message.messageType === MESSAGE_TYPE_FILE) {
            const link = document.createElement("a");
            link.href = attachmentUrl(message.id, true);
            link.className = "chat-attachment-file text-decoration-none text-body";

            const icon = document.createElement("i");
            icon.setAttribute("data-lucide", "file");
            icon.className = "chat-attachment-file-icon";
            link.appendChild(icon);

            const info = document.createElement("span");
            info.className = "chat-attachment-file-info";
            const nameEl = document.createElement("span");
            nameEl.className = "chat-attachment-file-name";
            nameEl.textContent = message.originalFileName || "";
            const sizeEl = document.createElement("span");
            sizeEl.className = "chat-attachment-file-size";
            sizeEl.textContent = formatFileSize(message.fileSizeBytes);
            info.appendChild(nameEl);
            info.appendChild(sizeEl);
            link.appendChild(info);

            const downloadIcon = document.createElement("i");
            downloadIcon.setAttribute("data-lucide", "download");
            downloadIcon.className = "flex-shrink-0";
            link.appendChild(downloadIcon);

            window.lucide?.createIcons();
            return link;
        }

        const textEl = document.createElement("p");
        textEl.className = "mb-1 chat-message-text";
        textEl.style.whiteSpace = "pre-wrap";
        textEl.textContent = message.text || "";
        return textEl;
    }

    function attachmentUrl(messageId, download) {
        return "/Student/Chat/Attachment?messageId=" + messageId + (download ? "&download=true" : "");
    }

    function formatFileSize(bytes) {
        if (!bytes) {
            return "";
        }

        const kb = bytes / 1024;
        return kb < 1024 ? kb.toFixed(1) + " KB" : (kb / 1024).toFixed(1) + " MB";
    }

    function startInlineEdit(bubble, message, currentUserId, conversationId) {
        const existingForm = bubble.querySelector(".chat-inline-edit-form");
        if (existingForm) {
            existingForm.querySelector("input").focus();
            return;
        }

        const form = document.createElement("form");
        form.className = "d-flex gap-2 mt-2 chat-inline-edit-form";

        const input = document.createElement("input");
        input.type = "text";
        input.className = "form-control form-control-sm";
        input.maxLength = 2000;
        input.value = message.text || "";
        form.appendChild(input);

        const saveBtn = document.createElement("button");
        saveBtn.type = "submit";
        saveBtn.className = "btn btn-sm btn-light";
        saveBtn.textContent = "Save";
        form.appendChild(saveBtn);

        form.addEventListener("submit", function (event) {
            event.preventDefault();
            const text = input.value.trim();
            if (!text) {
                return;
            }

            window.chatConnection?.invoke("EditMessage", conversationId, message.id, text)
                .then(function () {
                    form.remove();
                })
                .catch(function (err) {
                    console.error("Failed to edit message:", err);
                });
        });

        bubble.querySelector(".chat-message-body").appendChild(form);
        input.focus();
    }

    function formatTime(isoString) {
        const date = new Date(isoString);
        if (Number.isNaN(date.getTime())) {
            return "";
        }

        const datePart = date.toLocaleDateString(undefined, { month: "short", day: "numeric" });
        const timePart = date.toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit" });
        return datePart + ", " + timePart;
    }
})();
