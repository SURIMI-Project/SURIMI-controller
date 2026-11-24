window.surimiHcrDialog = {
    show: dialog => {
        dialog.onkeydown = event => {
            if (event.key === "Enter" && event.target instanceof HTMLInputElement && event.target.type === "number") {
                event.preventDefault();
            }
        };
        dialog.showModal();
    },
    close: dialog => dialog.close()
};
