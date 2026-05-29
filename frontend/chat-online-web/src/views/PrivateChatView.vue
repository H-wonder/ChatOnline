<template>
  <div class="pm-page">
    <!-- 顶部 -->
    <header class="pm-topbar glass-strong">
      <button class="btn btn-ghost btn-sm" @click="router.back()">← 返回</button>
      <div class="pm-title">
        <div class="pm-avatar" :style="{ background: avColor(targetUserId) }">
          {{ peerName[0] || '?' }}
        </div>
        <div class="pm-info">
          <div class="pm-name">{{ peerName }}</div>
          <div class="pm-status">
            <span v-if="requesting">⏳ 等待对方接受</span>
            <span v-else-if="chatStore.privateChatId && revealed" class="status-revealed">✓ 双方已暴露身份</span>
            <span v-else-if="chatStore.privateChatId">🔒 匿名私聊中</span>
            <span v-else>未连接</span>
          </div>
        </div>
      </div>
      <button v-if="chatStore.privateChatId && !revealed"
        class="btn btn-primary btn-sm" @click="handleReveal">
        🪪 暴露身份
      </button>
    </header>

    <!-- 消息区 -->
    <main class="pm-main">
      <div ref="scroller" class="msg-scroller">
        <div class="msg-list">
          <!-- 等待中 -->
          <div v-if="requesting" class="waiting">
            <div class="wait-pulse"></div>
            <p>正在等待对方接受私聊请求...</p>
            <p class="muted">如果对方拒绝，你将无法继续发送消息</p>
          </div>

          <!-- 空状态 -->
          <div v-else-if="!chatStore.privateMessages.length && chatStore.privateChatId" class="empty">
            <div class="empty-icon">💌</div>
            <p>对话开始啦，发送第一条消息吧</p>
          </div>

          <!-- 消息列表 -->
          <template v-for="(msg, idx) in chatStore.privateMessages" :key="msg.id">
            <div v-if="shouldShowTime(idx)" class="time-divider">
              <span>{{ formatTimeDivider(msg.createdAt) }}</span>
            </div>

            <div class="msg-row" :class="{ mine: msg.senderId === myId }">
              <div v-if="msg.senderId !== myId" class="msg-avatar"
                :style="{ background: avColor(msg.senderId) }">
                {{ (msg.senderName || peerName)[0] || '?' }}
              </div>
              <div class="msg-bubble" :class="{ mine: msg.senderId === myId }">
                <!-- 文本 -->
                <div v-if="!msg.fileUrl" class="bubble-text">{{ msg.content }}</div>
                <!-- 图片 -->
                <img v-else-if="isImageMsg(msg)" :src="resolveUrl(msg.fileUrl)"
                  :alt="msg.content || '图片'"
                  class="bubble-image" @click="previewUrl = resolveUrl(msg.fileUrl)" />
                <!-- 文件 -->
                <a v-else :href="resolveUrl(msg.fileUrl)" target="_blank" class="bubble-file">
                  📎 {{ msg.content || '文件' }}
                </a>
                <div class="bubble-time">{{ time(msg.createdAt) }}</div>
              </div>
            </div>
          </template>
        </div>
      </div>

      <!-- 输入栏 -->
      <div v-if="chatStore.privateChatId" class="composer glass-strong">
        <input ref="fileInput" type="file" hidden
          accept="image/*,.pdf,.doc,.docx,.txt,.zip,.rar,.mp3,.mp4"
          @change="handleFileSelect" />
        <button class="btn-icon" @click="fileInput?.click()" title="发送文件">📎</button>
        <textarea v-model="inputText" class="composer-input"
          placeholder="说点什么..." rows="1"
          @input="autoResize" @keydown.enter.exact.prevent="handleSend"
          @keydown.enter.shift.exact="(e)=>e.stopPropagation()"
          ref="textareaRef"></textarea>
        <button class="btn btn-primary send-btn"
          :disabled="(!inputText.trim() && !uploading) || uploading"
          @click="handleSend">
          {{ uploading ? '⏳' : '发送' }}
        </button>
      </div>
    </main>

    <!-- 收到的私聊请求弹窗 -->
    <Modal v-if="requestVisible" title="收到私聊请求" @close="requestVisible = false">
      <div class="req-body">
        <div class="req-avatar"
          :style="{ background: avColor(chatStore.privateChatRequest?.fromUserId) }">
          {{ chatStore.privateChatRequest?.fromUsername?.[0] || '?' }}
        </div>
        <h3 class="req-name">{{ chatStore.privateChatRequest?.fromUsername }}</h3>
        <p class="req-tip">向你发起了私聊请求</p>
        <p class="muted">默认匿名状态，双方都可以选择是否暴露身份</p>
      </div>
      <template #footer>
        <button class="btn btn-ghost" @click="handleReject">拒绝</button>
        <button class="btn btn-primary" @click="handleAccept">接受</button>
      </template>
    </Modal>

    <!-- 图片预览 -->
    <div v-if="previewUrl" class="img-preview" @click="previewUrl = null">
      <img :src="previewUrl" alt="图片预览" />
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useChatStore } from '@/stores/chatStore'
import { uploadFile } from '@/api/messageApi'
import { toast } from '@/utils/toast.js'
import Modal from '@/components/Modal.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const chatStore = useChatStore()

const myId = computed(() => authStore.user?.id)
const targetUserId = computed(() => Number.parseInt(route.query.userId))
const peerName = computed(() => route.query.nickname || '匿名用户')

const inputText = ref('')
const uploading = ref(false)
const requesting = ref(false)
const revealed = ref(false)
const requestVisible = ref(false)
const previewUrl = ref(null)
const scroller = ref(null)
const fileInput = ref(null)
const textareaRef = ref(null)

const avColors = [
  'linear-gradient(135deg,#fbcfe8,#f9a8d4)',
  'linear-gradient(135deg,#bae6fd,#7dd3fc)',
  'linear-gradient(135deg,#fde68a,#fbbf24)',
  'linear-gradient(135deg,#c4b5fd,#a78bfa)',
  'linear-gradient(135deg,#a7f3d0,#34d399)',
  'linear-gradient(135deg,#fda4af,#fb7185)'
]
function avColor(id) { return avColors[(id || 0) % avColors.length] }

function time(t) {
  if (!t) return ''
  return new Date(t).toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
}
function formatTimeDivider(t) {
  if (!t) return ''
  const d = new Date(t)
  const now = new Date()
  const isToday = d.toDateString() === now.toDateString()
  if (isToday) return d.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
  return d.toLocaleString('zh-CN', { month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}
function shouldShowTime(idx) {
  if (idx === 0) return true
  const cur = chatStore.privateMessages[idx]
  const prev = chatStore.privateMessages[idx - 1]
  if (!cur?.createdAt || !prev?.createdAt) return false
  return new Date(cur.createdAt) - new Date(prev.createdAt) > 5 * 60 * 1000
}
function isImageMsg(msg) {
  if (msg.messageType === 'Image' || msg.messageType === 1) return true
  return /\.(jpg|jpeg|png|gif|webp|bmp)$/i.test(msg.fileUrl || '')
}
function resolveUrl(url) {
  if (!url) return ''
  if (/^https?:\/\//i.test(url)) return url
  const base = (import.meta.env.VITE_API_BASE || 'http://localhost:5000').replace(/\/api\/?$/, '')
  return base + url
}

function autoResize() {
  const el = textareaRef.value
  if (!el) return
  el.style.height = 'auto'
  el.style.height = Math.min(el.scrollHeight, 120) + 'px'
}

function scrollBottom() {
  nextTick(() => {
    if (scroller.value) scroller.value.scrollTop = scroller.value.scrollHeight
  })
}

async function handleSend() {
  const text = inputText.value.trim()
  if (!text || !chatStore.privateChatId) return
  await chatStore.sendPrivate(chatStore.privateChatId, text)
  inputText.value = ''
  nextTick(() => { autoResize(); scrollBottom() })
}

async function handleFileSelect(e) {
  const file = e.target.files?.[0]
  if (!file || !chatStore.privateChatId) return
  uploading.value = true
  try {
    const res = await uploadFile(file)
    const isImage = /\.(jpg|jpeg|png|gif|webp|bmp)$/i.test(file.name)
    await chatStore.sendPrivate(chatStore.privateChatId, file.name, isImage ? 1 : 2, res.data.fileUrl)
    scrollBottom()
  } catch {
    toast.error('上传失败')
  }
  uploading.value = false
  e.target.value = ''
}

async function handleReveal() {
  await chatStore.reveal(chatStore.privateChatId)
  revealed.value = true
  toast.success('身份已暴露')
}
async function handleAccept() {
  await chatStore.acceptPrivateChat(chatStore.privateChatRequest.chatId, true)
  requestVisible.value = false
}
async function handleReject() {
  await chatStore.acceptPrivateChat(chatStore.privateChatRequest.chatId, false)
  requestVisible.value = false
  router.back()
}

watch(() => chatStore.privateChatRequest, (req) => { if (req) requestVisible.value = true })
watch(() => chatStore.privateChatId, (id) => { if (id) requesting.value = false })
watch(() => chatStore.privateMessages.length, scrollBottom)

onMounted(async () => {
  await chatStore.connect()  // 确保 SignalR 已连接
  if (targetUserId.value) {
    requesting.value = true
    await chatStore.startPrivateChat(targetUserId.value)
  }
})
</script>

<style scoped>
.pm-page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  padding: 16px 24px;
  gap: 12px;
}

/* 顶部 */
.pm-topbar {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 12px 18px;
  border-radius: var(--r-lg);
  flex-shrink: 0;
}
.btn-sm { padding: 7px 14px; font-size: 12px; }
.pm-title { flex: 1; display: flex; align-items: center; gap: 12px; }
.pm-avatar {
  width: 42px; height: 42px;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-weight: 700; color: #fff; font-size: 16px;
  box-shadow: 0 2px 8px rgba(0,0,0,.12);
}
.pm-name { font-size: 15px; font-weight: 600; }
.pm-status { font-size: 11px; color: var(--text-tertiary); margin-top: 2px; }
.status-revealed { color: #10b981; }

/* 消息主区 */
.pm-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 0;
}
.msg-scroller {
  flex: 1;
  overflow-y: auto;
  border-radius: var(--r-lg);
  background: rgba(255,255,255,.4);
  backdrop-filter: blur(10px);
  border: 1px solid var(--border-soft);
  padding: 24px;
}
.msg-list { max-width: 760px; margin: 0 auto; }

/* 等待 */
.waiting { text-align: center; padding: 80px 0; }
.wait-pulse {
  width: 56px; height: 56px;
  border-radius: 50%;
  background: var(--accent-gradient);
  margin: 0 auto 18px;
  animation: pulse 1.5s ease-in-out infinite;
}
@keyframes pulse {
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.15); opacity: .6; }
}
.waiting p { margin: 4px 0; color: var(--text-secondary); font-size: 13px; }
.muted { color: var(--text-tertiary); font-size: 12px; }

/* Empty */
.empty { text-align: center; padding: 80px 0; }
.empty-icon { font-size: 56px; margin-bottom: 12px; }
.empty p { color: var(--text-tertiary); font-size: 13px; margin: 0; }

/* 时间分隔 */
.time-divider {
  text-align: center;
  margin: 16px 0;
}
.time-divider span {
  display: inline-block;
  font-size: 11px;
  color: var(--text-tertiary);
  background: rgba(0,0,0,.04);
  padding: 3px 12px;
  border-radius: var(--r-full);
}

/* 消息行 */
.msg-row {
  display: flex;
  gap: 10px;
  margin-bottom: 14px;
  align-items: flex-end;
}
.msg-row.mine { flex-direction: row-reverse; }
.msg-avatar {
  width: 32px; height: 32px;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-weight: 700; color: #fff; font-size: 13px;
  flex-shrink: 0;
}
.msg-bubble {
  max-width: 480px;
  display: flex;
  flex-direction: column;
}
.msg-bubble.mine { align-items: flex-end; }

.bubble-text {
  background: rgba(255,255,255,.85);
  padding: 10px 14px;
  border-radius: 18px 18px 18px 4px;
  font-size: 14px;
  line-height: 1.55;
  word-break: break-word;
  color: var(--text-primary);
  box-shadow: 0 2px 8px rgba(0,0,0,.04);
}
.mine .bubble-text {
  background: var(--accent-gradient);
  color: #fff;
  border-radius: 18px 18px 4px 18px;
  box-shadow: 0 4px 12px rgba(99,102,241,.25);
}
.bubble-image {
  max-width: 280px;
  max-height: 280px;
  border-radius: var(--r-md);
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(0,0,0,.1);
}
.bubble-file {
  display: inline-flex; align-items: center; gap: 6px;
  background: rgba(255,255,255,.85);
  padding: 10px 14px;
  border-radius: var(--r-md);
  font-size: 13px;
  color: var(--accent);
  text-decoration: none;
}
.mine .bubble-file {
  background: var(--accent-gradient);
  color: #fff;
}
.bubble-time {
  font-size: 10px;
  color: var(--text-tertiary);
  margin-top: 4px;
  padding: 0 4px;
}

/* 输入栏 */
.composer {
  display: flex;
  gap: 10px;
  align-items: flex-end;
  padding: 12px 14px;
  border-radius: var(--r-lg);
  flex-shrink: 0;
}
.composer-input {
  flex: 1;
  border: none;
  background: transparent;
  outline: none;
  resize: none;
  font-size: 14px;
  font-family: inherit;
  line-height: 1.5;
  padding: 8px 4px;
  color: var(--text-primary);
  max-height: 120px;
}
.composer-input::placeholder { color: var(--text-tertiary); }
.send-btn { padding: 9px 22px; font-size: 13px; }

/* 请求弹窗 */
.req-body { text-align: center; }
.req-avatar {
  width: 64px; height: 64px;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 26px; font-weight: 700; color: #fff;
  margin: 0 auto 12px;
  box-shadow: 0 4px 12px rgba(0,0,0,.12);
}
.req-name { margin: 0 0 6px; font-size: 17px; }
.req-tip { margin: 0 0 8px; color: var(--text-secondary); font-size: 13px; }

/* 图片预览 */
.img-preview {
  position: fixed; inset: 0;
  background: rgba(0,0,0,.85);
  display: flex; align-items: center; justify-content: center;
  z-index: 200;
  cursor: zoom-out;
  padding: 24px;
  animation: fadeIn .2s ease;
}
.img-preview img {
  max-width: 100%;
  max-height: 100%;
  border-radius: var(--r-md);
}
@keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

@media (max-width: 640px) {
  .pm-page { padding: 8px 10px; gap: 8px; }
  .pm-topbar { padding: 10px 14px; }
  .pm-name { font-size: 14px; }
  .msg-scroller { padding: 14px; }
  .msg-bubble { max-width: 85%; }
  .bubble-text { font-size: 14px; padding: 9px 12px; }
  .composer { padding: 10px 12px; }
  .send-btn { padding: 8px 16px; font-size: 13px; }
}
</style>
