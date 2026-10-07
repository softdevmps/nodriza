<template>
    <v-card class="usuarios-card" elevation="2">

        <!-- HEADER -->
        <v-card-title class="d-flex align-center justify-space-between">
            <div class="d-flex align-center">
                <v-icon class="mr-2" color="primary">mdi-account-group</v-icon>
                <span class="text-h6 font-weight-medium">Listado de Usuarios</span>
            </div>

            <v-chip color="primary" variant="outlined" size="small">
                {{ usuarios.length }} usuarios
            </v-chip>
        </v-card-title>

        <v-divider />

        <!-- DATATABLE -->
        <v-data-table :headers="headers" :items="usuarios" class="usuarios-table" density="comfortable" hover>
            <!-- ACTIVO -->
            <template #item.activo="{ item }">
                <v-switch v-model="item.activo" inset color="green" hide-details
                    @change="$emit('cambiar-estado', item)" />
            </template>

            <!-- ACCIONES -->
            <template #item.actions="{ item }">
                <v-tooltip text="Editar usuario">
                    <template #activator="{ props }">
                        <v-btn v-bind="props" icon size="small" color="primary" variant="text"
                            @click="$emit('editar', item)">
                            <v-icon>mdi-pencil</v-icon>
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
        usuarios: {
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
.usuarios-card {
    border-radius: 12px;
}

.usuarios-table :deep(thead th) {
    font-weight: 600;
    text-transform: uppercase;
    font-size: 0.75rem;
    color: #6b7280;
}

.usuarios-table :deep(tbody tr:hover) {
    background-color: rgba(0, 0, 0, 0.02);
}
</style>
