var form = $("#submission");

var ids = [];

form.submit(function (e) {
  e.preventDefault();
  hide("downloadButton");
  $.ajax({
    type: "POST",
    url: "/crop",
    data: new FormData(form[0]),
    contentType: false,
    processData: false,
    success: function(data) {
      ids = JSON.parse(data);
      if (ids.length > 0) {
        show("downloadButton");
      }
    }
  });
});

function show(el) {
  document.getElementById(el).style.visibility = "visible";
}

function hide(el) {
  document.getElementById(el).style.visibility = "hidden";
}

function download() {
  var internal = document.getElementById("internal");
  ids.forEach(id => {
    internal.setAttribute("href", "crop/" + id);
    internal.click();
  });
}