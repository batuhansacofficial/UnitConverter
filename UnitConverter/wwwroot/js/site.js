(() => {
  "use strict";
  document.documentElement.classList.add("js");

  const storageKey = "unitwise.history.v1";
  const categoryIds = new Set([
    "length",
    "weight",
    "temperature",
    "area",
    "volume",
  ]);
  const status = document.querySelector("#app-status");
  const historyList = document.querySelector("[data-history-list]");
  const clearButton = document.querySelector("[data-clear-history]");
  const storageNote = document.querySelector("[data-storage-note]");
  let recent = [];
  let storageAvailable = true;
  let busy = false;

  const announce = (message) => {
    if (status) status.textContent = message;
  };
  const isEntry = (entry) =>
    entry &&
    typeof entry === "object" &&
    categoryIds.has(entry.category) &&
    [
      "categoryName",
      "from",
      "to",
      "input",
      "fromSymbol",
      "toSymbol",
      "output",
    ].every(
      (key) => typeof entry[key] === "string" && entry[key].length <= 150,
    );

  function storageUnavailable() {
    storageAvailable = false;
    if (storageNote)
      storageNote.textContent = "History is available for this visit only.";
  }

  try {
    const stored = JSON.parse(localStorage.getItem(storageKey) || "[]");
    if (Array.isArray(stored)) recent = stored.filter(isEntry).slice(0, 5);
  } catch {
    storageUnavailable();
  }

  function persistHistory() {
    if (!storageAvailable) return;
    try {
      localStorage.setItem(storageKey, JSON.stringify(recent));
    } catch {
      storageUnavailable();
    }
  }

  function icon(name) {
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.classList.add("icon");
    svg.setAttribute("aria-hidden", "true");
    const use = document.createElementNS("http://www.w3.org/2000/svg", "use");
    use.setAttribute("href", `#icon-${name}`);
    svg.append(use);
    return svg;
  }

  function renderHistory() {
    if (!historyList) return;
    historyList.replaceChildren();
    if (clearButton) clearButton.disabled = recent.length === 0;
    if (!recent.length) {
      const empty = document.createElement("div");
      empty.className = "empty-history";
      const title = document.createElement("p");
      title.textContent = "Your next conversion starts here.";
      const description = document.createElement("span");
      description.textContent = "Convert a value to keep it close.";
      empty.append(icon("restore"), title, description);
      historyList.append(empty);
      return;
    }
    recent.forEach((entry, index) => {
      const button = document.createElement("button");
      button.type = "button";
      button.className = "history-item";
      button.dataset.restore = String(index);
      button.setAttribute(
        "aria-label",
        `Restore ${entry.input} ${entry.fromSymbol} to ${entry.toSymbol}`,
      );
      const content = document.createElement("span");
      const kind = document.createElement("span");
      kind.className = "history-kind";
      kind.textContent = entry.categoryName;
      const value = document.createElement("span");
      value.className = "history-value";
      value.textContent = `${entry.input} ${entry.fromSymbol} → ${entry.output} ${entry.toSymbol}`;
      content.append(kind, value);
      button.append(content, icon("restore"));
      historyList.append(button);
    });
  }

  function rememberResult() {
    const result = document.querySelector('[data-history][data-store="true"]');
    if (!result) return;
    try {
      const entry = JSON.parse(result.dataset.history);
      if (!isEntry(entry)) return;
      recent = [
        entry,
        ...recent.filter(
          (item) =>
            !(
              item.category === entry.category &&
              item.from === entry.from &&
              item.to === entry.to &&
              item.input === entry.input
            ),
        ),
      ].slice(0, 5);
      persistHistory();
      renderHistory();
      result.dataset.store = "false";
    } catch {
      /* Optional history never prevents a conversion. */
    }
  }

  function revealActiveCategory(
    panel = document.querySelector("[data-workspace]"),
  ) {
    const nav = panel?.querySelector(".category-nav");
    const active = nav?.querySelector('[aria-current="page"]');
    if (nav && active && nav.scrollWidth > nav.clientWidth) {
      nav.scrollLeft = Math.max(
        0,
        active.offsetLeft - (nav.clientWidth - active.offsetWidth) / 2,
      );
    }
  }

  async function navigateWorkspace(url, options = {}, focus = "result") {
    if (busy) return;
    busy = true;
    const panel = document.querySelector("[data-workspace]");
    panel?.setAttribute("aria-busy", "true");
    if (panel) panel.inert = true;
    announce("Converting…");
    try {
      const response = await fetch(url, {
        ...options,
        headers: { "X-Requested-With": "fetch" },
      });
      if (!response.ok) throw new Error("Request failed");
      const html = new DOMParser().parseFromString(
        await response.text(),
        "text/html",
      );
      const nextPanel = html.querySelector("[data-workspace]");
      if (!nextPanel || !panel) throw new Error("Unexpected response");
      panel.replaceWith(nextPanel);
      revealActiveCategory(nextPanel);
      document.title = html.title;
      rememberResult();
      const error = nextPanel.querySelector("[data-error-summary]");
      if (error) {
        error.focus();
        announce("Check the highlighted fields and try again.");
      } else if (focus === "category") {
        nextPanel
          .querySelector("[data-category-heading]")
          ?.focus({ preventScroll: true });
        announce(
          `${nextPanel.querySelector("[data-category-heading]")?.textContent} selected.`,
        );
      } else {
        const result = nextPanel.querySelector("[data-history]");
        const entry = result ? JSON.parse(result.dataset.history) : null;
        announce(
          entry
            ? `${entry.input} ${entry.fromSymbol} is ${entry.output} ${entry.toSymbol}.`
            : "Conversion complete.",
        );
        const submit = nextPanel.querySelector(".convert-button");
        submit?.focus({ preventScroll: true });
      }
      return true;
    } catch {
      if (panel?.isConnected) panel.inert = false;
      announce("The server could not be reached. Please try again.");
      let error = document.querySelector("[data-network-error]");
      if (!error && panel?.isConnected) {
        error = document.createElement("div");
        error.className = "validation-summary";
        error.dataset.networkError = "";
        error.setAttribute("role", "alert");
        error.tabIndex = -1;
        error.textContent =
          "We couldn’t reach the server. Your input is still here. Please try again.";
        panel.querySelector(".form-heading")?.after(error);
      }
      error?.focus();
      return false;
    } finally {
      busy = false;
      document.querySelector("[data-workspace]")?.removeAttribute("aria-busy");
      const currentPanel = document.querySelector("[data-workspace]");
      if (currentPanel) currentPanel.inert = false;
    }
  }

  document.addEventListener("submit", async (event) => {
    const form = event.target;
    if (
      !(form instanceof HTMLFormElement) ||
      !form.matches("[data-convert-form]")
    )
      return;
    event.preventDefault();
    if (busy) return;
    const action = event.submitter?.formAction || form.action;
    await navigateWorkspace(action, {
      method: "POST",
      body: new FormData(form),
    });
  });

  document.addEventListener("input", (event) => {
    if (!event.target.closest("[data-convert-form]")) return;
    const result = document.querySelector("[data-result-area]");
    if (!result) return;
    result.classList.add("is-stale");
    const label = result.querySelector("[data-result-label]");
    if (label) label.textContent = "Previous result · convert to update";
    const copy = result.querySelector("[data-copy]");
    if (copy) copy.disabled = true;
  });

  document.addEventListener("click", async (event) => {
    const category = event.target.closest("[data-category]");
    if (
      category &&
      !event.ctrlKey &&
      !event.metaKey &&
      !event.shiftKey &&
      !event.altKey
    ) {
      event.preventDefault();
      const changed = await navigateWorkspace(category.href, {}, "category");
      if (changed) history.replaceState(null, "", category.href);
      return;
    }
    const restore = event.target.closest("[data-restore]");
    if (restore) {
      const entry = recent[Number(restore.dataset.restore)];
      if (!entry) return;
      const params = new URLSearchParams({
        Category: entry.category,
        From: entry.from,
        To: entry.to,
        Value: entry.input,
      });
      const changed = await navigateWorkspace(`/?${params}`);
      if (changed) history.replaceState(null, "", `/?${params}`);
      return;
    }
    if (event.target.closest("[data-clear-history]")) {
      recent = [];
      persistHistory();
      renderHistory();
      announce("Recent conversions cleared.");
      document
        .querySelector("#history-heading")
        ?.setAttribute("tabindex", "-1");
      document
        .querySelector("#history-heading")
        ?.focus({ preventScroll: true });
      return;
    }
    const copy = event.target.closest("[data-copy]");
    if (copy && !copy.disabled) {
      const text = copy.querySelector("span");
      try {
        await navigator.clipboard.writeText(copy.dataset.copy);
        if (text) text.textContent = "Copied";
        announce("Result copied to clipboard.");
      } catch {
        if (text) text.textContent = "Select to copy";
        const value = document.querySelector(".result-number");
        if (value) {
          const range = document.createRange();
          range.selectNodeContents(value);
          const selection = window.getSelection();
          selection.removeAllRanges();
          selection.addRange(range);
        }
        announce(
          "Clipboard is unavailable. The result is selected; copy it with your keyboard.",
        );
      }
      setTimeout(() => {
        if (text?.isConnected) text.textContent = "Copy result";
      }, 2200);
    }
    if (event.target.closest("[data-open-help]")) {
      const dialog = document.querySelector("#how-it-works");
      if (dialog?.showModal) {
        event.preventDefault();
        dialog.showModal();
      }
    }
  });

  const help = document.querySelector("#how-it-works");
  help?.addEventListener("click", (event) => {
    if (event.target === help) {
      const rect = help.getBoundingClientRect();
      if (
        event.clientX < rect.left ||
        event.clientX > rect.right ||
        event.clientY < rect.top ||
        event.clientY > rect.bottom
      )
        help.close();
    }
  });

  // Native anchor fallback makes help available even when JavaScript is disabled.
  if (location.hash === "#how-it-works" && help?.showModal) help.showModal();
  renderHistory();
  rememberResult();
  revealActiveCategory();
})();
