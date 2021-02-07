window.ace.config.set(
  "basePath",
  "https://cdnjs.cloudflare.com/ajax/libs/ace/1.4.11/"
);
window.ace.require("ace/ext/language_tools");


Vue.component('Editor', {
	template: '<div ref="editor" style="width: 100%; height: 25em;"></div>',
  props: ['editorId', 'value'],
  data () {
    return {
      editor: Object,
      beforeContent: ''
    }
  },
  watch: {
    'value' (value) {
    	if (this.beforeContent !== value) {
      	this.editor.setValue(value, 1)
      }
    }
  },
  mounted () {
  
		this.editor = window.ace.edit(this.$refs.editor)
    this.editor.setValue(this.value, 1)
    
    this.editor.getSession().setMode(`ace/mode/javascript`)
    this.editor.setTheme(`ace/theme/monokai`)

    this.editor.on('change', () => {
    	this.beforeContent = this.editor.getValue()
      this.$emit('input', this.beforeContent)
    })
  }
})
const co = {
  props: ["fg", "bg"],
  computed: {
    style() {
      return {
        color: "#" + this.fg,
        "background-color": "#" + this.bg,
      };
    },
  },
  template: '<span :style="style"><slot/></span>',
};
const to = {
  props: ["to"],
  methods: {
    invoke() {
      if (this.$root.programId == null) return;
      var topic = this.to;
      var data = "";
      if (this.to[0] == "?") {
        topic = this.to.substring(1);
        data = prompt("Please enter input...", "");
      }
      axios.get("/computer/topic", {
        params: {
          id: this.$root.programId,
          topic: topic,
          data: data,
        }
      })
    },
  },
  template:
    '<span style="cursor: pointer;" @click.prevent="invoke"><slot/></span>',
};

Vue.component('debugger', {

})

const scriptPresets = {
  Computer: `Term.write("AAAA")
if(5 * 3)
  Term.write(2^8)`
}

var app = new Vue({
  el: "#app",
  data: {
    programType: null,
    programId: null,
    script: '',
    console_buffer: null,
    console_buffer_auto_update: true,
    alert: {
      type: '',
      message: null,
      tid: null
    }
  },
  computed: {
    term() {
      if (this.console_buffer == null) return {}
      return {
        components: {
          co,
          to,
        },
        template: `<div class="terminal">${this.console_buffer}</div>`,
      };
    },
  },
  methods: {
    a(type = '', message = null) {
      this.alert.type = type
      this.alert.message = message
      if(type) {
        if(this.alert.tid) {
          clearTimeout(this.alert.tid)
        }
        this.alert.tid = setTimeout(() => this.a(), 3000)
      }
    },
    createProgram(type = 'Computer') {
      axios.get("/new_program", {
        params: {
          type
        }
      }).then(r => {
        this.programId = r.data
        this.programType = type
        this.loadPreset(type)
        this.a('success', `Created program with id (${this.programId}).`)
      })
    },
    loadPreset(type = 'Computer') {
      this.script = scriptPresets[type] || 'debugger;'
    },
    execute() {
      if (this.programId == null) return
      axios.get("/execute", {
        params: {
          id: this.programId,
          code: this.script
        }
      }).then(r => {
        if(!r.data)
          return
        this.a('success', 'Executed script.')
      })
      .catch(() => {
        this.a('error', 'Script execution failed.')
      })
    },
    update_console_buffer() {
      axios.get("/computer/get_buffer", {
        params: {
          id: this.programId
        }
      }).then(r => {
        this.console_buffer = r.data
      })
      .catch(() => {
        this.a('error', 'Failed to update console buffer.')
      })
    },
    set_debugging(state = 1) {
      axios.get("/debug/set", {
        params: {
          id: this.programId,
          state: state
        }
      }).then(r => {
        if(state)
          this.a('success', 'Enabled debugging.')
        else
          this.a('success', 'Disabled debugging.')
      })
      .catch(() => {
        this.a('error', 'Failed to change debugging state.')
      })
    },
    remove() {
      if (this.programId == null) return
      axios.get("/remove", {
        params: {
          id: this.programId
        }
      }).then(r => {
        console.log(r);
        this.programId = null
        this.programType = null
        this.console_buffer = null
        this.a('success', 'Program successfully disposed of.')
      })
      .catch(() => {
        this.a('error', 'There was a problem while removing program.')
      })
    },
    reset() {
      axios.get("/clear").then(() => {
        this.programId = null
        this.programType = null
        this.console_buffer = null
        this.a('success', 'Daemon state successfully cleared.')
      })
      .catch(() => {
        this.a('error', 'There was a problem while resting daemon state.')
      })
    },
    fire() {
      if(this.programType == 'Computer' && this.console_buffer_auto_update) {
        this.update_console_buffer()
      }
    }
  },
  vuetify: new Vuetify(),
});

setInterval(app.fire, 1000);