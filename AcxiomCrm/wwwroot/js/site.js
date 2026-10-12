document.addEventListener("DOMContentLoaded", function () {
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebar = document.getElementById("sidebar");
    const backdrop = document.getElementById("sidebarBackdrop");

    if (sidebarToggle && sidebar && backdrop) {
        sidebarToggle.addEventListener("click", function () {
            sidebar.classList.toggle("show");
            backdrop.classList.toggle("show");
        });

        backdrop.addEventListener("click", function () {
            sidebar.classList.remove("show");
            backdrop.classList.remove("show");
        });
    }

    if (window.jQuery && window.jQuery.validator && window.jQuery.validator.unobtrusive) {
        window.jQuery.validator.addMethod("futureortoday", function (value, element) {
            if (!value) return true;
            const inputDate = new Date(value);
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            return inputDate >= today;
        });
        window.jQuery.validator.unobtrusive.adapters.addBool("futureortoday");

        window.jQuery.validator.addMethod("notpastdate", function (value, element) {
            if (!value) return true;
            const inputDate = new Date(value);
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            return inputDate >= today;
        });
        window.jQuery.validator.unobtrusive.adapters.addBool("notpastdate");
    }
});
