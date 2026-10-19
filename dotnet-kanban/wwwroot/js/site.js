// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

const taskModal = document.getElementById("task-modal");

function openTaskView() {
    if (!taskModal) return;
    taskModal.classList.remove("hidden");
    taskModal.classList.add("flex");
    taskModal.setAttribute("aria-hidden", "false");
}

function closeTaskView() {
    if (!taskModal) return;
    taskModal.classList.add("hidden");
    taskModal.classList.remove("flex");
    taskModal.setAttribute("aria-hidden", "true");
}

document.body.addEventListener("openTaskView", openTaskView);
document.body.addEventListener("closeTaskView", closeTaskView);

let draggedGroup = null;
let originalGroupOrder = [];
let draggedTask = null;
let originalTaskOrders = [];

function dragStartElement(event) {
    return document.elementFromPoint(event.clientX, event.clientY);
}

function restoreGroupOrder(groupList, order) {
    for (const group of order) groupList.append(group);
}

function restoreTaskOrders(orders) {
    for (const [list, cards] of orders) {
        for (const card of cards) list.append(card);
    }
}

async function postDragForm(form, fields) {
    const data = new FormData(form);
    for (const [name, value] of fields) data.append(name, value);

    const response = await fetch(form.action, {
        method: "POST",
        body: data,
        headers: { "HX-Request": "true" }
    });
    if (!response.ok) throw new Error(`The server rejected the new order (HTTP ${response.status}).`);
}

function showDragSaveError(error) {
    console.error(error);
    window.alert("The new order could not be saved. The previous order has been restored.");
}

function reorderGroupAtPointer(groupList, target, pointerX) {
    const targetGroup = target.closest("[data-group-card]");
    if (!targetGroup || targetGroup === draggedGroup || targetGroup.parentElement !== groupList) return;

    const bounds = targetGroup.getBoundingClientRect();
    const insertAfter = pointerX > bounds.left + bounds.width / 2;
    groupList.insertBefore(draggedGroup, insertAfter ? targetGroup.nextElementSibling : targetGroup);
}

function reorderTaskAtPointer(taskList, target, pointerY) {
    const targetCard = target.closest("[data-task-card]");
    if (!targetCard || targetCard === draggedTask || targetCard.parentElement !== taskList) {
        if (!targetCard && target === taskList) taskList.append(draggedTask);
        return;
    }

    const bounds = targetCard.getBoundingClientRect();
    const insertAfter = pointerY > bounds.top + bounds.height / 2;
    taskList.insertBefore(draggedTask, insertAfter ? targetCard.nextElementSibling : targetCard);
}

document.addEventListener("dragstart", (event) => {
    const target = event.target;
    if (!(target instanceof Element)) return;
    const startElement = dragStartElement(event);
    if (!(startElement instanceof Element)) return;

    const groupHandle = target.closest("[data-group-drag-handle]");
    if (groupHandle) {
        if (startElement.closest("details, button, input, select, textarea, form, summary")) {
            event.preventDefault();
            return;
        }

        const groupList = groupHandle.closest("[data-group-list]");
        const group = groupHandle.closest("[data-group-card]");
        if (!(groupList instanceof HTMLElement) || !(group instanceof HTMLElement)) return;

        draggedGroup = group;
        originalGroupOrder = Array.from(groupList.children);
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", group.dataset.groupId ?? "");
        return;
    }

    const taskCard = target.closest("[data-task-card]");
    if (!taskCard) return;
    if (startElement.closest("button, a, input, select, textarea, form, details, summary")) {
        event.preventDefault();
        return;
    }

    const taskList = taskCard.closest("[data-task-list]");
    if (!(taskList instanceof HTMLElement)) return;

    draggedTask = taskCard;
    originalTaskOrders = Array.from(document.querySelectorAll("[data-task-list]"))
        .map((list) => [list, Array.from(list.children)]);
    event.dataTransfer.effectAllowed = "move";
    event.dataTransfer.setData("text/plain", taskCard.dataset.taskId ?? "");
});

document.addEventListener("dragover", (event) => {
    const target = event.target;
    if (!(target instanceof Element)) return;

    if (draggedGroup) {
        const groupList = target.closest("[data-group-list]");
        if (!groupList) return;
        event.preventDefault();
        event.dataTransfer.dropEffect = "move";
        reorderGroupAtPointer(groupList, target, event.clientX);
        return;
    }

    if (draggedTask) {
        const taskList = target.closest("[data-task-list]");
        if (!taskList) return;
        event.preventDefault();
        event.dataTransfer.dropEffect = "move";
        reorderTaskAtPointer(taskList, target, event.clientY);
    }
});

document.addEventListener("drop", async (event) => {
    const target = event.target;
    if (!(target instanceof Element)) return;

    if (draggedGroup) {
        const groupList = target.closest("[data-group-list]");
        if (!groupList) return;
        event.preventDefault();

        const movedGroup = draggedGroup;
        const previousOrder = originalGroupOrder;
        draggedGroup = null;
        originalGroupOrder = [];
        const groupIds = Array.from(groupList.querySelectorAll(":scope > [data-group-card]"))
            .map((group) => group.dataset.groupId)
            .filter((groupId) => groupId);

        try {
            await postDragForm(
                document.getElementById("reorder-groups-form"),
                groupIds.map((groupId) => ["groupIds", groupId])
            );
        } catch (error) {
            restoreGroupOrder(groupList, previousOrder);
            showDragSaveError(error);
        }
        return;
    }

    if (draggedTask) {
        const destinationList = target.closest("[data-task-list]");
        if (!destinationList) return;
        event.preventDefault();

        const movedTask = draggedTask;
        const previousOrders = originalTaskOrders;
        draggedTask = null;
        originalTaskOrders = [];
        const cards = Array.from(destinationList.querySelectorAll(":scope > [data-task-card]"));
        const destinationPosition = cards.indexOf(movedTask);

        try {
            await postDragForm(document.getElementById("move-task-form"), [
                ["taskId", movedTask.dataset.taskId],
                ["destinationGroupId", destinationList.dataset.groupId],
                ["destinationPosition", String(destinationPosition)]
            ]);
        } catch (error) {
            restoreTaskOrders(previousOrders);
            showDragSaveError(error);
        }
    }
});

document.addEventListener("dragend", () => {
    if (draggedGroup) restoreGroupOrder(draggedGroup.closest("[data-group-list]"), originalGroupOrder);
    if (draggedTask) restoreTaskOrders(originalTaskOrders);
    draggedGroup = null;
    originalGroupOrder = [];
    draggedTask = null;
    originalTaskOrders = [];
});

function appendSelectedTaskTag(selectedTags, tagId, title, colour, formId) {
    if (selectedTags.querySelector(`[data-tag-id="${CSS.escape(tagId)}"]`)) return;

    const tag = document.createElement("span");
    tag.dataset.tagId = tagId;
    tag.className = "inline-flex items-center gap-1 rounded-full px-3.5 py-1 text-base font-semibold uppercase leading-5 text-white";
    tag.style.backgroundColor = colour;

    const hiddenInput = document.createElement("input");
    hiddenInput.type = "hidden";
    hiddenInput.name = "tagIds";
    hiddenInput.value = tagId;
    hiddenInput.setAttribute("form", formId);

    const label = document.createElement("span");
    label.textContent = title;

    const removeButton = document.createElement("button");
    removeButton.type = "button";
    removeButton.dataset.removeTaskTag = "";
    removeButton.className = "ml-1 rounded-full text-white/80 hover:text-white";
    removeButton.setAttribute("aria-label", `Remove ${title} tag`);
    removeButton.textContent = "×";

    tag.append(hiddenInput, label, removeButton);
    selectedTags.append(tag);
}

document.addEventListener("change", (event) => {
    const picker = event.target;
    if (!(picker instanceof HTMLSelectElement) || !picker.matches("[data-task-tag-picker]")) return;

    const option = picker.selectedOptions[0];
    const controls = picker.closest("[data-task-tag-controls]");
    const selectedTags = picker.closest("section")?.querySelector("[data-selected-task-tags]");
    const editForm = picker.closest("section")?.querySelector("form[id^='task-edit-form-']");
    if (!option || !option.value || !controls || !(selectedTags instanceof HTMLElement) || !(editForm instanceof HTMLFormElement)) return;

    appendSelectedTaskTag(selectedTags, option.value, option.textContent ?? "", option.dataset.colour ?? "", editForm.id);
    option.disabled = true;
    picker.value = "";
});

document.addEventListener("click", (event) => {
    const target = event.target;
    if (!(target instanceof Element)) return;

    document.querySelectorAll("details[data-task-tag-popup][open]").forEach((popup) => {
        if (!popup.contains(target)) popup.open = false;
    });

    if (target.closest("[data-close-task-view]")) {
        closeTaskView();
        return;
    }

    if (target === taskModal) {
        closeTaskView();
        return;
    }

    const dueDateAction = target.closest("[data-due-date-action]");
    if (dueDateAction) {
        const container = dueDateAction.closest("[data-due-date-control]");
        const input = container?.querySelector("[data-due-date-input]");
        const addButton = container?.querySelector('[data-due-date-action="add"]');
        const removeButton = container?.querySelector('[data-due-date-action="remove"]');
        if (!(input instanceof HTMLInputElement)) return;

        if (dueDateAction.dataset.dueDateAction === "add") {
            input.classList.remove("hidden");
            addButton?.classList.add("hidden");
            removeButton?.classList.remove("hidden");
            input.focus();
        } else {
            input.value = "";
            input.classList.add("hidden");
            addButton?.classList.remove("hidden");
            removeButton?.classList.add("hidden");
        }
        return;
    }

    const removeTagButton = target.closest("[data-remove-task-tag]");
    if (removeTagButton) {
        const tag = removeTagButton.closest("[data-tag-id]");
        const section = removeTagButton.closest("section");
        const picker = section?.querySelector("[data-task-tag-picker]");
        if (tag instanceof HTMLElement && picker instanceof HTMLSelectElement) {
            const option = picker.querySelector(`option[value="${CSS.escape(tag.dataset.tagId ?? "")}"]`);
            if (option instanceof HTMLOptionElement) option.disabled = false;
        }
        tag?.remove();
    }
});

document.body.addEventListener("htmx:afterSwap", (event) => {
    const target = event.detail?.target;
    if (!(target instanceof HTMLElement) || !target.matches("[data-selected-task-tags]")) return;

    const section = target.closest("section");
    const picker = section?.querySelector("[data-task-tag-picker]");
    if (!(picker instanceof HTMLSelectElement)) return;

    const addedTag = target.lastElementChild;
    const tagId = addedTag instanceof HTMLElement ? addedTag.dataset.tagId : undefined;
    if (!tagId) return;

    let option = picker.querySelector(`option[value="${CSS.escape(tagId)}"]`);
    if (!(option instanceof HTMLOptionElement) && addedTag instanceof HTMLElement) {
        option = document.createElement("option");
        option.value = tagId;
        option.textContent = addedTag.dataset.tagTitle ?? "";
        option.dataset.colour = addedTag.dataset.tagColour ?? "";
        picker.append(option);
    }
    if (option instanceof HTMLOptionElement) option.disabled = true;
});

document.addEventListener("keydown", (event) => {
    if (event.key === "Escape" && taskModal && !taskModal.classList.contains("hidden")) {
        closeTaskView();
    }
});
