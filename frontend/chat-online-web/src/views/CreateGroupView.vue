<template>
  <div class="cg-page">
    <header class="cg-topbar glass-strong">
      <button class="btn btn-ghost btn-sm" @click="$router.push('/lobby')">← 返回</button>
      <h2 class="cg-title">创建群聊</h2>
      <div style="width: 60px"></div>
    </header>

    <main class="cg-main">
      <form class="cg-form glass-strong fade-in" @submit.prevent="handleCreate">
        <div class="form-header">
          <div class="form-icon">✨</div>
          <h3>新建一个群聊</h3>
          <p>填写下面的信息，开启你的匿名社区</p>
        </div>

        <!-- 群名称 -->
        <div class="field">
          <label>群名称 <span class="required">*</span></label>
          <input class="input" v-model="form.name"
            placeholder="给你的群取个好听的名字" maxlength="20" />
          <span class="hint">{{ form.name.length }}/20</span>
        </div>

        <!-- 群简介 -->
        <div class="field">
          <label>群简介</label>
          <textarea class="input" v-model="form.description"
            placeholder="简单介绍一下这个群的话题（选填）"
            rows="3" maxlength="200"></textarea>
          <span class="hint">{{ form.description.length }}/200</span>
        </div>

        <!-- 可见性 -->
        <div class="field">
          <label>可见性</label>
          <div class="radio-group">
            <label class="radio-card" :class="{ active: form.isPublic }">
              <input type="radio" v-model="form.isPublic" :value="true" />
              <div class="r-icon">🌍</div>
              <div class="r-info">
                <div class="r-title">公开群</div>
                <div class="r-desc">出现在大厅，所有人都能看到</div>
              </div>
              <div class="r-check">✓</div>
            </label>
            <label class="radio-card" :class="{ active: !form.isPublic }">
              <input type="radio" v-model="form.isPublic" :value="false" />
              <div class="r-icon">🔒</div>
              <div class="r-info">
                <div class="r-title">私有群</div>
                <div class="r-desc">仅通过群号加入，不在大厅显示</div>
              </div>
              <div class="r-check">✓</div>
            </label>
          </div>
        </div>

        <!-- 高级选项 -->
        <div class="advanced-toggle" @click="showAdvanced = !showAdvanced">
          <span>{{ showAdvanced ? '▼' : '▶' }} 高级设置</span>
          <span class="muted">入群密码、入群问答（选填）</span>
        </div>

        <div v-if="showAdvanced" class="advanced-section">
          <div class="field">
            <label>入群密码</label>
            <input class="input" type="password" v-model="form.password"
              placeholder="留空表示无需密码" />
          </div>

          <div class="field">
            <label>入群问题</label>
            <input class="input" v-model="form.question"
              placeholder="例如：我们群的暗号是？" />
          </div>

          <div class="field" v-if="form.question">
            <label>正确答案 <span class="required">*</span></label>
            <input class="input" v-model="form.questionAnswer"
              placeholder="只有回答正确才能加入" />
          </div>
        </div>

        <button type="submit" :disabled="creating" class="btn btn-primary submit-btn">
          {{ creating ? '创建中...' : '🚀 创建群聊' }}
        </button>
      </form>
    </main>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { createGroup } from '@/api/groupApi'
import { toast } from '@/utils/toast.js'

const router = useRouter()
const creating = ref(false)
const showAdvanced = ref(false)
const form = reactive({
  name: '', description: '', isPublic: true,
  password: '', question: '', questionAnswer: ''
})

async function handleCreate() {
  if (!form.name.trim()) { toast.warning('请输入群名称'); return }
  if (form.question && !form.questionAnswer.trim()) {
    toast.warning('请填写问题的正确答案'); return
  }
  creating.value = true
  try {
    const res = await createGroup({
      name: form.name,
      description: form.description || null,
      isPublic: form.isPublic,
      password: form.password || null,
      question: form.question || null,
      questionAnswer: form.questionAnswer || null
    })
    toast.success('创建成功')
    router.push(`/group/${res.data.id}`)
  } catch (err) {
    const msg = err?.response?.data?.errors?.Password?.[0]
      || err?.response?.data?.message
    toast.error(msg || '创建失败')
  }
  creating.value = false
}
</script>

<style scoped>
.cg-page { min-height: 100vh; padding-bottom: 40px; }

.cg-topbar {
  display: flex; align-items: center; justify-content: space-between;
  gap: 16px;
  padding: 12px 24px;
  margin: 16px 24px 0;
  border-radius: var(--r-lg);
}
.cg-title { margin: 0; font-size: 16px; font-weight: 600; }
.btn-sm { padding: 7px 14px; font-size: 12px; }

.cg-main {
  max-width: 560px;
  margin: 32px auto 0;
  padding: 0 24px;
}

.cg-form {
  border-radius: var(--r-xl);
  padding: 32px 28px;
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.form-header {
  text-align: center;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--border-soft);
  margin-bottom: 4px;
}
.form-icon {
  width: 60px; height: 60px;
  margin: 0 auto 12px;
  background: var(--accent-gradient);
  border-radius: var(--r-lg);
  display: flex; align-items: center; justify-content: center;
  font-size: 30px;
  box-shadow: 0 8px 24px rgba(99,102,241,.3);
}
.form-header h3 {
  margin: 0 0 6px;
  font-size: 18px;
  font-weight: 600;
}
.form-header p {
  margin: 0;
  color: var(--text-secondary);
  font-size: 13px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  position: relative;
}
.field label {
  font-size: 13px;
  color: var(--text-secondary);
  font-weight: 500;
  margin-left: 4px;
}
.required { color: #ef4444; margin-left: 2px; }
.hint {
  position: absolute;
  bottom: 8px; right: 12px;
  font-size: 11px;
  color: var(--text-tertiary);
  pointer-events: none;
}
textarea.input { resize: vertical; min-height: 76px; padding-bottom: 22px; }

/* 单选卡片 */
.radio-group {
  display: flex;
  gap: 12px;
}
.radio-card {
  flex: 1;
  position: relative;
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 14px;
  background: rgba(255,255,255,.5);
  border: 2px solid transparent;
  border-radius: var(--r-md);
  cursor: pointer;
  transition: all .2s;
}
.radio-card:hover {
  background: rgba(255,255,255,.7);
}
.radio-card.active {
  border-color: var(--accent);
  background: rgba(99,102,241,.08);
  box-shadow: 0 4px 12px rgba(99,102,241,.15);
}
.radio-card input[type="radio"] { display: none; }
.r-icon {
  width: 36px; height: 36px;
  display: flex; align-items: center; justify-content: center;
  background: rgba(255,255,255,.8);
  border-radius: var(--r-sm);
  font-size: 18px;
  flex-shrink: 0;
}
.r-info { flex: 1; min-width: 0; }
.r-title { font-size: 14px; font-weight: 600; }
.r-desc { font-size: 11px; color: var(--text-tertiary); margin-top: 2px; }
.r-check {
  position: absolute;
  top: 8px; right: 10px;
  width: 18px; height: 18px;
  border-radius: 50%;
  background: var(--accent-gradient);
  color: #fff;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px;
  font-weight: 700;
  opacity: 0;
  transform: scale(.6);
  transition: all .2s;
}
.radio-card.active .r-check {
  opacity: 1;
  transform: scale(1);
}

/* 高级设置 */
.advanced-toggle {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 10px 14px;
  background: rgba(0,0,0,.03);
  border-radius: var(--r-md);
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  color: var(--text-secondary);
  transition: background .15s;
}
.advanced-toggle:hover { background: rgba(0,0,0,.05); }
.muted { color: var(--text-tertiary); font-size: 11px; font-weight: 400; }

.advanced-section {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 4px 2px;
  animation: slideDown .2s ease;
}
@keyframes slideDown {
  from { opacity: 0; transform: translateY(-6px); }
  to { opacity: 1; transform: translateY(0); }
}

.submit-btn {
  margin-top: 8px;
  padding: 14px;
  font-size: 14px;
  width: 100%;
  font-weight: 600;
}

@media (max-width: 640px) {
  .cg-topbar { margin: 8px 10px 0; padding: 10px 14px; }
  .cg-main { padding: 0 10px; margin-top: 20px; }
  .cg-form { padding: 24px 18px; }
  .radio-group { flex-direction: column; }
}
</style>
