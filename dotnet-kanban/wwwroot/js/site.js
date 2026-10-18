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
