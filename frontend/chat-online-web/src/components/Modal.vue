<template>
  <Teleport to="body">
    <div class="modal-mask" @click.self="$emit('close')">
      <div class="modal-card glass-strong fade-in">
        <header class="modal-head">
          <h3>{{ title }}</h3>
          <button class="btn-icon" @click="$emit('close')">✕</button>
        </header>
        <div class="modal-body">
          <slot />
        </div>
        <footer v-if="$slots.footer" class="modal-foot">
          <slot name="footer" />
        </footer>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
defineProps({ title: String })
defineEmits(['close'])
</script>

<style scoped>
.modal-mask {
  position: fixed; inset: 0;
  background: rgba(15,15,30,.4);
  backdrop-filter: blur(8px);
  display: flex; align-items: center; justify-content: center;
  z-index: 100;
  padding: 24px;
  animation: fadeIn .2s ease;
}
.modal-card {
  width: 100%; max-width: 440px;
  border-radius: var(--r-lg);
  box-shadow: var(--shadow-xl);
  display: flex; flex-direction: column;
  max-height: 90vh;
}
.modal-head {
  display: flex; align-items: center; justify-content: space-between;
  padding: 18px 20px 14px;
  border-bottom: 1px solid var(--border-soft);
}
.modal-head h3 { margin: 0; font-size: 16px; font-weight: 600; }
.modal-body { padding: 20px; overflow-y: auto; flex: 1; }
.modal-foot {
  display: flex; justify-content: flex-end; gap: 10px;
  padding: 14px 20px 18px;
  border-top: 1px solid var(--border-soft);
}
@keyframes fadeIn { from { opacity: 0; transform: scale(.95); } to { opacity: 1; transform: scale(1); } }
</style>
