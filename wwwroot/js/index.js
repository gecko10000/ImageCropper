var form = $("#submission");
form.submit(function (e) {
  e.preventDefault();
  $.ajax({
    type: "POST",
    url: "/crop",
    data: new FormData(form[0]),
    contentType: false,
    processData: false,
    success: function(data) {
      console.log(data);
    }
  });
});
