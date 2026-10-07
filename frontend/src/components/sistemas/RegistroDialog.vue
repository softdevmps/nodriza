<template>
  <v-dialog v-model="model" max-width="640px" scrollable>
    <v-card class="dialog-card sb-dialog">
      <v-card-title class="sb-dialog-title">
        <div class="sb-dialog-icon">
          <v-icon color="primary">
            {{ isEdit ? 'mdi-pencil' : isDuplicate ? 'mdi-content-copy' : 'mdi-plus-box' }}
          </v-icon>
        </div>
        <div>
          <div class="sb-dialog-title-text">
            {{ dialogTitle }}
          </div>
          <div class="sb-dialog-subtitle">
            Completa los campos requeridos para continuar.
          </div>
        </div>
      </v-card-title>

      <v-divider />

      <v-card-text class="sb-dialog-body sb-dialog-scroll">
        <v-form class="form sb-form">
          <template v-if="layout === 'tabs'">
            <v-tabs v-model="activeTab" density="compact">
              <v-tab v-for="(group, index) in fieldGroups" :key="group.name" :value="index">
                {{ group.name }}
              </v-tab>
            </v-tabs>
            <v-window v-model="activeTab">
              <v-window-item v-for="(group, index) in fieldGroups" :key="group.name" :value="index">
                <v-row v-for="field in group.fields" :key="field.columnName" class="sb-form-row sb-form-grid" dense>
                  <v-col cols="12">
                    <component
                      :is="resolveInputType(field) === 'select' ? 'v-select'
                        : resolveInputType(field) === 'textarea' ? 'v-textarea'
                        : resolveInputType(field) === 'number' ? 'v-text-field'
                        : resolveInputType(field) === 'date' ? 'v-text-field'
                        : resolveInputType(field) === 'datetime' ? 'v-text-field'
                        : resolveInputType(field) === 'checkbox' ? 'v-checkbox'
                        : resolveInputType(field) === 'switch' ? 'v-switch'
                        : 'v-text-field'"
                      v-model="form[field.columnName]"
                      :items="resolveInputType(field) === 'select' ? fkOptions?.[field.columnName]?.options : undefined"
                      item-title="title"
                      item-value="value"
                      :label="field.label || field.name || field.columnName"
                      :placeholder="field.placeholder || undefined"
                      :hint="field.helpText || undefined"
                      persistent-hint
                      :maxlength="field.maxLength || undefined"
                      :type="resolveInputType(field) === 'number' ? 'number' : resolveInputType(field) === 'date' ? 'date' : resolveInputType(field) === 'datetime' ? 'datetime-local' : undefined"
                      clearable
                      :density="density"
                      :rules="rulesFor(field)"
                      no-data-text="Sin registros"
                      :append-inner-icon="resolveInputType(field) === 'select' ? 'mdi-plus' : undefined"
                      @click:append-inner="() => resolveInputType(field) === 'select' && crearFk(field)"
                    />
                  </v-col>
                </v-row>
              </v-window-item>
            </v-window>
          </template>

          <template v-else-if="layout === 'sections'">
            <v-card v-for="group in fieldGroups" :key="group.name" class="mb-3 sb-form-section" elevation="0">
              <v-card-title class="text-subtitle-2">{{ group.name }}</v-card-title>
              <v-card-text>
                <v-row v-for="field in group.fields" :key="field.columnName" class="sb-form-row sb-form-grid" dense>
                  <v-col cols="12">
                    <component
                      :is="resolveInputType(field) === 'select' ? 'v-select'
                        : resolveInputType(field) === 'textarea' ? 'v-textarea'
                        : resolveInputType(field) === 'number' ? 'v-text-field'
                        : resolveInputType(field) === 'date' ? 'v-text-field'
                        : resolveInputType(field) === 'datetime' ? 'v-text-field'
                        : resolveInputType(field) === 'checkbox' ? 'v-checkbox'
                        : resolveInputType(field) === 'switch' ? 'v-switch'
                        : 'v-text-field'"
                      v-model="form[field.columnName]"
                      :items="resolveInputType(field) === 'select' ? fkOptions?.[field.columnName]?.options : undefined"
                      item-title="title"
                      item-value="value"
                      :label="field.label || field.name || field.columnName"
                      :placeholder="field.placeholder || undefined"
                      :hint="field.helpText || undefined"
                      persistent-hint
                      :maxlength="field.maxLength || undefined"
                      :type="resolveInputType(field) === 'number' ? 'number' : resolveInputType(field) === 'date' ? 'date' : resolveInputType(field) === 'datetime' ? 'datetime-local' : undefined"
                      clearable
                      :density="density"
                      :rules="rulesFor(field)"
                      no-data-text="Sin registros"
                      :append-inner-icon="resolveInputType(field) === 'select' ? 'mdi-plus' : undefined"
                      @click:append-inner="() => resolveInputType(field) === 'select' && crearFk(field)"
                    />
                  </v-col>
                </v-row>
              </v-card-text>
            </v-card>
          </template>

          <template v-else>
            <v-row v-for="field in editableFields" :key="field.columnName" class="sb-form-row sb-form-grid" dense>
              <v-col cols="12">
                <component
                  :is="resolveInputType(field) === 'select' ? 'v-select'
                    : resolveInputType(field) === 'textarea' ? 'v-textarea'
                    : resolveInputType(field) === 'number' ? 'v-text-field'
                    : resolveInputType(field) === 'date' ? 'v-text-field'
                    : resolveInputType(field) === 'datetime' ? 'v-text-field'
                    : resolveInputType(field) === 'checkbox' ? 'v-checkbox'
                    : resolveInputType(field) === 'switch' ? 'v-switch'
                    : 'v-text-field'"
                  v-model="form[field.columnName]"
                  :items="resolveInputType(field) === 'select' ? fkOptions?.[field.columnName]?.options : undefined"
                  item-title="title"
                  item-value="value"
                  :label="field.label || field.name || field.columnName"
                  :placeholder="field.placeholder || undefined"
                  :hint="field.helpText || undefined"
                  persistent-hint
                  :maxlength="field.maxLength || undefined"
                  :type="resolveInputType(field) === 'number' ? 'number' : resolveInputType(field) === 'date' ? 'date' : resolveInputType(field) === 'datetime' ? 'datetime-local' : undefined"
                  clearable
                  :density="density"
                  :rules="rulesFor(field)"
                  no-data-text="Sin registros"
                  :append-inner-icon="resolveInputType(field) === 'select' ? 'mdi-plus' : undefined"
                  @click:append-inner="() => resolveInputType(field) === 'select' && crearFk(field)"
                />
              </v-col>
            </v-row>
          </template>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="pa-4 sb-dialog-actions">
        <v-spacer />
        <v-btn class="sb-btn ghost" variant="text" :density="density" @click="cerrar">Cancelar</v-btn>
        <v-btn class="sb-btn primary" color="primary" :density="density" @click="guardar">Guardar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import datosService from '../../api/datos.service.js';

export default {
  props: {
    modelValue: Boolean,
    record: Object,
    fields: Array,
    fkOptions: Object,
    layout: {
      type: String,
      default: 'single'
    },
    density: {
      type: String,
      default: 'comfortable'
    },
    messages: {
      type: Object,
      default: () => ({})
    },
    confirmSave: {
      type: Boolean,
      default: true
    },
    mode: {
      type: String,
      default: 'create'
    },
    systemId: Number,
    entityId: Number
  },

  emits: ['update:modelValue', 'guardado', 'crear-fk'],

  data() {
    return {
      form: {},
      activeTab: 0
    };
  },

  computed: {
    model: {
      get() {
        return this.modelValue;
      },
      set(v) {
        this.$emit('update:modelValue', v);
      }
    },

    editableFields() {
      if (!this.fields) return [];
      return this.fields.filter(f => {
        if (f.isIdentity) return false;
        if (this.isEdit && f.isPrimaryKey) return false;
        return true;
      });
    },

    pkField() {
      return this.fields?.find(f => f.isPrimaryKey);
    },

    isEdit() {
      return this.mode === 'edit' && !!this.record;
    },

    isDuplicate() {
      return this.mode === 'duplicate';
    },

    dialogTitle() {
      if (this.isEdit) return 'Editar registro';
      if (this.isDuplicate) return 'Duplicar registro';
      return 'Nuevo registro';
    },

    fieldGroups() {
      const groups = {};
      this.editableFields.forEach(field => {
        const section = field.section || 'General';
        if (!groups[section]) groups[section] = [];
        groups[section].push(field);
      });
      return Object.entries(groups).map(([name, fields]) => ({ name, fields }));
    }
  },

  watch: {
    record: {
      immediate: true,
      handler(value) {
        const next = {};
        this.editableFields.forEach(field => {
          if (this.isEdit || this.isDuplicate) {
            next[field.columnName] = value ? value[field.columnName] ?? '' : '';
          } else {
            next[field.columnName] = '';
          }
          if (field.dataType === 'bool') {
            next[field.columnName] = value ? !!value[field.columnName] : false;
          }
        });
        this.form = next;
      }
    },

    layout() {
      this.activeTab = 0;
    },

    fields: {
      immediate: true,
      handler() {
        if (!this.record) {
          const next = {};
          this.editableFields.forEach(field => {
            next[field.columnName] = field.dataType === 'bool' ? false : '';
          });
          this.form = next;
        }
      }
    }
  },

  methods: {
    resolveInputType(field) {
      const forced = (field.inputType || '').toLowerCase();
      if (forced && forced !== 'auto') return forced;
      if (this.fkOptions?.[field.columnName]) return 'select';
      if (field.dataType === 'bool') return 'switch';
      if (field.dataType === 'datetime') return 'datetime';
      if (field.dataType === 'int' || field.dataType === 'decimal') return 'number';
      return 'text';
    },

    rulesFor(field) {
      const rules = [];
      if (field.required) {
        rules.push(v => (v !== null && v !== undefined && v !== '') || 'Requerido');
      }
      if (field.min !== null && field.min !== undefined) {
        rules.push(v => {
          if (v === null || v === undefined || v === '') return true;
          if (field.dataType === 'string') return v.length >= field.min || `Min ${field.min}`;
          return Number(v) >= Number(field.min) || `Min ${field.min}`;
        });
      }
      if (field.max !== null && field.max !== undefined) {
        rules.push(v => {
          if (v === null || v === undefined || v === '') return true;
          if (field.dataType === 'string') return v.length <= field.max || `Max ${field.max}`;
          return Number(v) <= Number(field.max) || `Max ${field.max}`;
        });
      }
      if (field.pattern) {
        try {
          const regex = new RegExp(field.pattern);
          rules.push(v => {
            if (v === null || v === undefined || v === '') return true;
            return regex.test(v) || 'Formato invalido';
          });
        } catch {
          // ignore invalid regex
        }
      }
      return rules;
    },

    crearFk(field) {
      this.$emit('crear-fk', field);
    },

    guardar() {
      if (this.confirmSave) {
        const ok = window.confirm('Guardar cambios?');
        if (!ok) return;
      }

      const payload = { ...this.form };

      if (this.isEdit && !this.pkField) {
        window.alert('Entidad sin PK, no se puede editar.');
        return;
      }

      const req = this.isEdit
        ? datosService.editar(this.systemId, this.entityId, this.record[this.pkField.columnName], payload)
        : datosService.crear(this.systemId, this.entityId, payload);

      req.then(() => {
        this.$emit('guardado');
        const message = this.isEdit
          ? this.messages?.successUpdate
          : this.isDuplicate
            ? this.messages?.successCreate
            : this.messages?.successCreate;
        if (message) window.alert(message);
        this.cerrar();
      }).catch(error => {
        const message =
          error?.response?.data?.message ||
          error?.response?.data ||
          error?.message ||
          this.messages?.error ||
          'Error al guardar.';
        window.alert(message);
      });
    },

    cerrar() {
      this.model = false;
    }
  }
};
</script>

<style scoped>
.dialog-card {
  border-radius: 14px;
}

.form :deep(.v-field) {
  margin-bottom: 4px;
}
</style>
