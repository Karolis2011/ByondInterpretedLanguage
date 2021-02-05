var programId = null;
window.vueapp = {};
function console_log(o) {
  if (typeof o != "string") o = JSON.stringify(o);
  $("#output").append(`[${Date()}] ${o}\r\n`);
}

function create_program_com() {
  if (programId != null) return;
  $.ajax({
    type: "GET",
    url: "/new_program",
    data: {
      type: "Computer",
    },
    success: (data) => {
      console_log("Program id: " + data);
      programId = data;
      get_buffer();
    },
  });
}

function create_program_tcom() {
  if (programId != null) return;
  $.ajax({
    type: "GET",
    url: "/new_program",
    data: {
      type: "TCom",
    },
    success: (data) => {
      console_log("Program id: " + data);
      programId = data;
    },
  });
}

function exec_program() {
  if (programId == null) return;
  $.ajax({
    type: "GET",
    url: `/execute`,
    data: {
      id: programId,
      code: editor.getValue(),
    },
    success: (data) => {
      console_log(data);
      get_buffer()
    },
  });
}

function remove_program() {
  if (programId == null) return;
  $.ajax({
    type: "GET",
    url: `/remove`,
    data: {
      id: programId,
    },
    success: (data) => {
      window.vueapp.$data.buffer = "";
      $("#output").text("");
      console_log("Program removed. " + data)
      programId = null;
    },
  });
}

function get_buffer() {
  if (programId == null) return;
  $.ajax({
    type: "GET",
    url: `/computer/get_buffer`,
    data: {
      id: programId,
    },
    success: (data) => {
      window.vueapp.$data.buffer = data;
    },
  });
}

function reset() {
  if (programId == null) return;
  $.ajax({
    type: "GET",
    url: `/clear`,
    success: (data) => {
      window.vueapp.$data.buffer = "";
      $("#output").text("");
      programId = null;
      console_log(data)
    },
  });
}
