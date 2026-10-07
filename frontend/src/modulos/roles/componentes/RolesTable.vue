<template>
    <v-card class="roles-card" elevation="2">

        <!-- HEADER -->
        <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
                <v-icon class="mr-2" color="primary">mdi-shield-account</v-icon>
                <span class="text-h6 font-weight-medium">Listado de Roles</span>
            </div>

            <v-chip color="primary" variant="outlined" size="small">
                {{ roles.length }} roles
            </v-chip>
        </v-card-title>

        <v-divider />

        <!-- DATATABLE -->
        <v-data-table :headers="headers" :items="roles" class="roles-table" density="comfortable" hover>
            <!-- ACTIVO -->
            <template #item.activo="{ item }">
                <v-switch v-model="item.activo" inset color="green" hide-details
                    @change="$emit('cambiar-estado', item)" />
            </template>

            <!-- ACCIONES -->
            <template #item.actions="{ item }">
                <v-tooltip text="Editar rol">
                    <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="primary" variant="text"
                            @click="$emit('editar', item)">
                            <v-icon>mdi-pencil</v-icon>
                        </v-btn>
                    </template>
                </v-tooltip>

                <v-tooltip text="Asignar menús">
                    <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="secondary" variant="text"
                            @click="$emit('menus', item)">
                            <v-icon>mdi-menu</v-icon>
                        </v-btn>
                    </template>
                </v-tooltip>

                <v-tooltip text="Asignar sistemas">
                    <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="teal" variant="text"
                            @click="$emit('system-menus', item)">
                            <v-icon>mdi-apps</v-icon>
                        </v-btn>
                    </template>
                </v-tooltip>

                <v-tooltip text="Permisos">
                    <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="indigo" variant="text"
                            @click="$emit('permisos', item)">
                            <v-icon>mdi-shield-key</v-icon>
                        </v-btn>
                    </template>
                </v-tooltip>
            </template>
        </v-data-table>
    </v-card>
</template>

<script>
export default {
    props: {
        roles: {
            type: Array,
            required: true
        },
        headers: {
            type: Array,
            required: true
        }
    }
};
</script>

<style scoped>
.roles-card {
    border-radius: 12px;
}

.roles-table :deep(thead th) {
    font-weight: 600;
    text-transform: uppercase;
    font-size: 0.75rem;
    color: #6b7280;
}

.roles-table :deep(tbody tr:hover) {
    background-color: rgba(0, 0, 0, 0.02);
}
</style>
