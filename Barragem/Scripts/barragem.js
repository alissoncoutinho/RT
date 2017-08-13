var myApp;
myApp = myApp || (function () {
    var pleaseWaitDiv = $('#WaitDialog');
    return {
        showPleaseWait: function () {
            pleaseWaitDiv.modal();
        },
        hidePleaseWait: function () {
            pleaseWaitDiv.modal('hide');
        },

    };
})();

$(document).ready(function () {

    $.fn.datepicker.defaults.format = "dd/mm/yyyy";

    $('.alert .close').on("click", function (e) {
        $(this).parent().hide();
    });
    
    $(".confirmDialog").confirm({
        title: "Confirmar Operação",
        text: "Tem certeza que deseja realizar esta operação?",
        confirm: function (button) {
            window.location.href = button.data("link");
        },
        cancel: function (button) {
            
        },
        confirmButton: "Sim",
        cancelButton: "Não"
    });
    
});


