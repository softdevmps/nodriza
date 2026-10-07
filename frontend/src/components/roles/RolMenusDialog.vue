<template>
    <v-dialog :model-value="modelValue" @update:model-value="cerrar" max-width="520">
        <v-card class="rol-menus-card">

            <!-- HEADER -->
            <v-card-title class="d-flex align-center">
                <v-icon class="mr-2" color="primary">
                    mdi-menu
                </v-icon>
                <span class="text-h6 font-weight-medium">
                    Menús del rol: {{ rol?.nombre }}
                </span>
            </v-card-title>

            <v-divider />

            <!-- BODY -->
            <v-card-text class="rol-menus-body">
                <v-row dense>
                    <v-col cols="12" v-for="menu in menus" :key="menu.id">
                        <v-checkbox v-model="menu.asignado" :label="menu.titulo" color="primary" hide-details />
                    </v-col>
                </v-row>
            </v-card-text>

            <v-divider />

            <!-- ACTIONS -->
            <v-card-actions class="pa-4">
                <v-spacer />
                <v-btn variant="text" @click="cerrar">
                    Cancelar
                </v-btn>

                <v-btn color="primary" @click="guardar">
                    Guardar
                </v-btn>
            </v-card-actions>

        </v-card>
    </v-dialog>
</template>

<script>
import rolService from '../../api/rol.service.js';
import { useMenuStore } from '../../store/menu.store.js';

export default {
    props: {
        modelValue: {
            type: Boolean,
            required: true
        },
        rol: {
            type: Object,
            required: true
        }
    },

    emits: ['update:modelValue'],

    data() {
        return {
            menus: []
        };
    },

    watch: {
        modelValue(value) {
            if (value && this.rol) {
                this.cargarMenus();
            }
        }
    },

    methods: {
        async cargarMenus() {
            const res = await rolService.getById(this.rol.id);
            this.menus = res.data.menus;
        },

        cerrar() {
            this.$emit('update:modelValue', false);
        },

        async guardar() {
            const menusIds = this.menus
                .filter(m => m.asignado)
                .map(m => Number(m.id));

            await rolService.asignarMenus(this.rol.id, menusIds);
            const { cargarMenuTree } = useMenuStore();
            await cargarMenuTree();
            this.cerrar();
        }
    }
};
</script>

<style scoped>
.rol-menus-card {
    border-radius: 14px;
}

.rol-menus-body {
    padding-top: 12px;
    padding-bottom: 12px;
    max-height: 350px;
    overflow-y: auto;
}

/* Scroll prolijo */
.rol-menus-body::-webkit-scrollbar {
    width: 6px;
}

.rol-menus-body::-webkit-scrollbar-thumb {
    background-color: rgba(0, 0, 0, 0.2);
    border-radius: 4px;
}
</style>
