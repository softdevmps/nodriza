<template>
  <v-dialog v-model="model" max-width="720px">
    <v-card class="dialog-card">
      <v-card-title class="d-flex align-center">
        <v-icon class="mr-2" color="primary">
          {{ relacion ? 'mdi-link-variant' : 'mdi-link-plus' }}
        </v-icon>
        <span class="text-h6 font-weight-medium">
          {{ relacion ? 'Editar relacion' : 'Nueva relacion' }}
        </span>
      </v-card-title>

      <v-divider />

      <v-card-text>
        <v-form class="form">
          <v-row>
            <v-col cols="12" md="6">
              <v-select
                v-model="form.sourceEntityId"
                :items="entidadesOptions"
                item-title="title"
                item-value="value"
                label="Entidad origen"
                :disabled="!!relacion"
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-select
                v-model="form.targetEntityId"
                :items="entidadesOptions"
                item-title="title"
                item-value="value"
                label="Entidad destino"
                :disabled="!!relacion"
              />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="6">
              <v-select
                v-model="form.foreignKey"
                :items="camposOptions"
                item-title="title"
                item-value="value"
                label="Foreign key (campo)"
              />
            </v-col>
            <v-col cols="12" md="6">
              <v-select
                v-model="form.relationType"
                :items="relationTypes"
                item-title="title"
                item-value="value"
                label="Tipo de relacion"
              />
            </v-col>
          </v-row>

          <v-row>
            <v-col cols="12" md="6">
              <v-text-field
                v-model="form.inverseProperty"
                label="Inverse property (opcional)"
              />
            </v-col>
            <v-col cols="12" md="6" class="d-flex align-center">
              <v-switch
                v-model="form.cascadeDelete"
                color="red"
                label="Cascade delete"
              />
            </v-col>
          </v-row>
        </v-form>
      </v-card-text>

      <v-divider />

      <v-card-actions class="pa-4">
        <v-spacer />
        <v-btn variant="text" @click="cerrar">Cancelar</v-btn>
        <v-btn color="primary" @click="guardar">Guardar</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import campoService from '../../api/campo.service.js'
import relacionService from '../../api/relacion.service.js'

export default {
  props: {
    modelValue: Boolean,
    relacion: Object,
    entidades: Array,
    systemId: Number
  },

  emits: ['update:modelValue', 'guardado'],

  data() {
    return {
      form: {
        sourceEntityId: null,
        targetEntityId: null,
        foreignKey: '',
        relationType: 'ManyToOne',
        inverseProperty: '',
        cascadeDelete: false
      },
      camposOptions: []
    }
  },

  computed: {
    model: {
      get() {
        return this.modelValue
      },
      set(v) {
        this.$emit('update:modelValue', v)
      }
    },

    entidadesOptions() {
      return (this.entidades || []).map(e => ({
        title: e.displayName || e.name,
        value: e.id
      }))
    },

    relationTypes() {
      return [
        { title: 'ManyToOne', value: 'ManyToOne' },
        { title: 'OneToMany', value: 'OneToMany' },
        { title: 'OneToOne', value: 'OneToOne' },
        { title: 'ManyToMany', value: 'ManyToMany' }
      ]
    }
  },

  watch: {
    relacion: {
      immediate: true,
      handler(value) {
        this.syncForm(value)
      }
    },
    modelValue(value) {
      if (!value) return
      // When opening, always sync to avoid stale data
      this.syncForm(this.relacion)
    },

    'form.sourceEntityId': {
      immediate: true,
      handler(value) {
        if (!value) {
          this.camposOptions = []
          return
        }
        this.cargarCampos(value)
      }
    }
  },

  methods: {
    reset() {
      this.form = {
        sourceEntityId: null,
        targetEntityId: null,
        foreignKey: '',
        relationType: 'ManyToOne',
        inverseProperty: '',
        cascadeDelete: false
      }
      this.camposOptions = []
    },

    syncForm(value) {
      if (!value) {
        this.reset()
        return
      }

      this.form = {
        sourceEntityId: value.sourceEntityId,
        targetEntityId: value.targetEntityId,
        foreignKey: value.foreignKey || '',
        relationType: value.relationType || 'ManyToOne',
        inverseProperty: value.inverseProperty || '',
        cascadeDelete: !!value.cascadeDelete
      }

      if (value.sourceEntityId) {
        this.cargarCampos(value.sourceEntityId)
      }
    },

    async cargarCampos(entityId) {
      if (!this.systemId || !entityId) return
      const { data } = await campoService.getByEntity(this.systemId, entityId)
      this.camposOptions = (data || []).map(f => ({
        title: `${f.columnName} (${f.dataType})`,
        value: f.columnName
      }))
    },

    async guardar() {
      if (!this.systemId) return

      const payload = {
        sourceEntityId: this.form.sourceEntityId,
        targetEntityId: this.form.targetEntityId,
        relationType: this.form.relationType,
        foreignKey: this.form.foreignKey,
        inverseProperty: this.form.inverseProperty,
        cascadeDelete: this.form.cascadeDelete
      }

      if (!payload.sourceEntityId || !payload.targetEntityId || !payload.relationType) {
        window.alert('Completa los datos requeridos.')
        return
      }

      if (this.relacion) {
        await relacionService.editar(this.systemId, this.relacion.id, {
          relationType: payload.relationType,
          foreignKey: payload.foreignKey,
          inverseProperty: payload.inverseProperty,
          cascadeDelete: payload.cascadeDelete
        })
      } else {
        await relacionService.crear(this.systemId, payload)
      }

      this.$emit('guardado')
      this.cerrar()
    },

    cerrar() {
      this.reset()
      this.model = false
    }
  }
}
</script>

<style scoped>
.dialog-card {
  border-radius: 14px;
}

.form :deep(.v-field) {
  margin-bottom: 4px;
}
</style>
