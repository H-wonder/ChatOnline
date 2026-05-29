<template>
  <div class="chat-page">
    <!-- 手机端侧边栏遮罩 -->
    <div v-if="sidebarOpen" class="sidebar-mask" @click="sidebarOpen = false"></div>

    <!-- 左侧：群列表 -->
    <aside class="sidebar glass-strong" :class="{ 'sidebar-open': sidebarOpen }">
      <div class="sidebar-head">
        <button class="btn btn-ghost back-btn" @click="$router.push('/lobby')">← 大厅</button>
      </div>
      <div class="sidebar-title">我的群聊</div>
      <div class="group-list">
        <div v-for="g in chatStore.myGroups" :key="g.id"
          class="group-item"
          :class="{ active: g.id === groupId }"
          @click="switchGroup(g.id); sidebarOpen = false">
          <div class="g-avatar" :style="{ background: gradient(g.id) }">{{ emoji(g.id) }}</div>
          <div class="g-info">
            <div class="g-name">{{ g.name }}</div>
            <div class="g-meta">#{{ g.id }} · {{ g.memberCount }} 人</div>
          </div>
        </div>
        <div v-if="!chatStore.myGroups.length" class="sidebar-empty">
          暂无群聊
        </div>
      </div>
    </aside>

    <!-- 右侧：聊天区 -->
    <main class="chat-main">
      <!-- 顶栏 -->
      <header class="chat-head glass-strong">
        <div class="head-info">
          <button class="btn-icon sidebar-toggle" @click="sidebarOpen = !sidebarOpen" title="群列表">☰</button>
          <div class="head-icon" :style="{ background: gradient(groupId) }">{{ emoji(groupId) }}</div>
          <div>
            <div class="head-name">{{ groupInfo?.name || '加载中...' }} <span class="card-id">#{{ groupId }}</span></div>
            <div class="head-sub">{{ groupInfo?.members?.length || 0 }} 名成员</div>
          </div>
        </div>
        <div class="head-actions">
          <button v-if="canManage" class="btn-icon" @click="showSettings = true" title="群设置">⚙️</button>
          <button class="btn-icon" :class="{ active: showMembers }" @click="showMembers = !showMembers" title="成员">👥</button>
          <button v-if="isOwner" class="btn btn-danger" @click="handleDisband">解散</button>
          <button v-else class="btn btn-ghost" @click="handleQuit">退出</button>
        </div>
      </header>

      <!-- 消息区域 -->
      <div class="messages-area" ref="msgArea" @scroll="onScroll">
        <div v-if="chatStore.loadingHistory" class="load-more">加载中...</div>
        <div v-else-if="chatStore.hasMoreHistory && chatStore.groupMessages.length"
          class="load-more clickable" @click="loadMore">↑ 加载更多</div>

        <div class="msg-list">
          <template v-for="(msg, idx) in chatStore.groupMessages" :key="msg.id">
            <!-- 时间分隔线 -->
            <div v-if="shouldShowTime(msg, chatStore.groupMessages[idx-1])" class="time-divider">
              <span>{{ formatDay(msg.createdAt) }}</span>
            </div>

            <!-- 系统消息 -->
            <div v-if="msg.messageType === 'System'" class="sys-msg">
              {{ msg.content }}
            </div>

            <!-- 普通消息 -->
            <div v-else class="msg-row" :class="{ mine: msg.senderId === myId, hideAvatar: isContinuation(msg, chatStore.groupMessages[idx-1]) }">
              <div class="msg-avatar" :style="{ background: avColor(msg.senderId) }">
                {{ (msg.senderNickname || '?')[0] }}
              </div>
              <div class="msg-body">
                <div v-if="!isContinuation(msg, chatStore.groupMessages[idx-1])" class="msg-meta">
                  <span class="msg-name">{{ msg.senderNickname }}</span>
                  <span class="msg-time">{{ formatTime(msg.createdAt) }}</span>
                </div>
                <div class="msg-bubble" :class="{ mine: msg.senderId === myId }">
                  <template v-if="msg.messageType === 'Text'">{{ msg.content }}</template>
                  <template v-else-if="msg.messageType === 'Image'">
                    <img :src="msg.fileUrl" class="msg-image" alt="图片" @click="previewImage(msg.fileUrl)" />
                  </template>
                  <template v-else-if="msg.messageType === 'File'">
                    <a :href="msg.fileUrl" target="_blank" class="msg-file">📎 {{ msg.content || '文件' }}</a>
                  </template>
                </div>
                <div v-if="msg.status" class="msg-status">
                  <span v-if="msg.status === 'sending'">●</span>
                  <span v-else-if="msg.status === 'failed'" class="status-failed">! 发送失败</span>
                  <span v-else>✓</span>
                </div>
              </div>
              <button v-if="msg.senderId !== myId && msg.messageType !== 'System'"
                class="pm-btn" @click="startPM(msg.senderId)">私聊</button>
            </div>
          </template>
        </div>
        <div ref="msgEnd"></div>

        <!-- 滚动到底部按钮 -->
        <button v-if="!atBottom" class="to-bottom-btn" @click="scrollBottom">↓</button>
      </div>

      <!-- 输入框 -->
      <div class="input-area glass-strong">
        <input ref="fileInput" type="file" style="display:none"
          accept="image/*,.pdf,.doc,.docx,.txt,.zip,.rar,.mp3,.mp4"
          @change="handleFile" />
        <button class="btn-icon" @click="$refs.fileInput.click()" :disabled="isMuted" title="附件">📎</button>
        <textarea ref="inputBox" class="msg-input"
          v-model="inputText"
          :placeholder="isMuted ? '你已被禁言' : '输入消息（Enter 发送，Shift+Enter 换行）'"
          :disabled="isMuted"
          @keydown="onKeyDown"
          @input="autoResize" rows="1"></textarea>
        <button class="btn btn-primary send-btn" @click="handleSend"
          :disabled="(!inputText.trim() && !uploading) || isMuted">
          {{ uploading ? '上传中' : '发送' }}
        </button>
      </div>
    </main>

    <!-- 成员侧栏 -->
    <aside v-if="showMembers" class="members-panel glass-strong">
      <div class="panel-head">
        <span>成员 · {{ groupInfo?.members?.length || 0 }}</span>
        <button class="btn-icon" @click="showMembers = false">✕</button>
      </div>
      <div class="member-list">
        <div v-for="m in groupInfo?.members" :key="m.userId" class="member-item">
          <div class="m-avatar" :style="{ background: avColor(m.userId) }">{{ m.anonNickname?.[0] }}</div>
          <div class="m-info">
            <div class="m-name">
              {{ m.anonNickname }}
              <span v-if="m.role === '群主'" class="m-tag owner">群主</span>
              <span v-else-if="m.role === '管理员'" class="m-tag admin">管理</span>
              <span v-if="m.isMuted" class="m-tag muted">禁言</span>
            </div>
          </div>
          <div v-if="canManage && m.userId !== myId && m.role !== '群主'" class="m-actions">
            <button class="btn-icon small" @click="handleMute(m.userId, !m.isMuted)" :title="m.isMuted ? '解禁' : '禁言'">
              {{ m.isMuted ? '🔊' : '🔇' }}
            </button>
            <button class="btn-icon small" @click="handleKick(m.userId)" title="踢出">🚪</button>
          </div>
          <button v-else-if="m.userId !== myId" class="btn-icon small" @click="startPM(m.userId)" title="私聊">💬</button>
        </div>
      </div>
    </aside>

    <!-- 群设置 -->
    <Modal v-if="showSettings" title="群聊设置" @close="showSettings = false">
      <div class="modal-form">
        <div class="field"><label>群名称</label>
          <input class="input" v-model="settingsForm.name" maxlength="20" />
        </div>
        <div class="field"><label>群简介</label>
          <textarea class="input" v-model="settingsForm.description" rows="2" maxlength="200"></textarea>
        </div>
        <div class="field"><label>入群密码（留空则无）</label>
          <input class="input" type="password" v-model="settingsForm.password" />
        </div>
        <div class="field"><label>入群问题</label>
          <input class="input" v-model="settingsForm.question" />
        </div>
        <div class="field" v-if="settingsForm.question"><label>问题答案</label>
          <input class="input" v-model="settingsForm.questionAnswer" />
        </div>
      </div>
      <template #footer>
        <button class="btn btn-ghost" @click="showSettings = false">取消</button>
        <button class="btn btn-primary" @click="saveSettings" :disabled="savingSettings">保存</button>
      </template>
    </Modal>

    <!-- 图片预览 -->
    <div v-if="previewUrl" class="img-preview" @click="previewUrl = ''">
      <img :src="previewUrl" alt="预览" />
    </div>

    <!-- 收到私聊请求弹窗 -->
    <Modal v-if="showPmRequest" title="收到私聊请求" @close="rejectPm">
      <div style="text-align:center; padding: 8px 0">
        <div class="pm-req-avatar" :style="{ background: avColor(chatStore.privateChatRequest?.fromUserId) }">
          {{ chatStore.privateChatRequest?.fromUsername?.[0] || '?' }}
        </div>
        <div style="font-size:16px; font-weight:600; margin: 10px 0 4px">
          {{ chatStore.privateChatRequest?.fromUsername }}
        </div>
        <div style="font-size:13px; color:var(--text-secondary)">向你发起了私聊请求</div>
      </div>
      <template #footer>
        <button class="btn btn-ghost" @click="rejectPm">拒绝</button>
        <button class="btn btn-primary" @click="acceptPm">接受</button>
      </template>
    </Modal>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount, nextTick, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useChatStore } from '@/stores/chatStore'
import { getGroupDetail, kickMember, toggleMute, leaveGroup, dissolveGroup, updateGroup } from '@/api/groupApi'
import { uploadFile } from '@/api/messageApi'
import { toast, confirm } from '@/utils/toast.js'
import Modal from '@/components/Modal.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const chatStore = useChatStore()

const groupId = computed(() => Number.parseInt(route.params.id))
const myId = computed(() => authStore.user?.id)

const groupInfo = ref(null)
const sidebarOpen = ref(false)
const inputText = ref('')
const uploading = ref(false)
const showMembers = ref(false)
const showSettings = ref(false)
const savingSettings = ref(false)
const settingsForm = ref({ name:'', description:'', password:'', question:'', questionAnswer:'' })
const msgArea = ref(null)
const msgEnd = ref(null)
const inputBox = ref(null)
const atBottom = ref(true)
const showPmRequest = ref(false)
const previewUrl = ref('')

const isMuted = computed(() => groupInfo.value?.members?.find(m => m.userId === myId.value)?.isMuted || false)
const canManage = computed(() => {
  const me = groupInfo.value?.members?.find(m => m.userId === myId.value)
  return me?.role === '群主' || me?.role === '管理员'
})
const isOwner = computed(() => groupInfo.value?.members?.find(m => m.userId === myId.value)?.role === '群主')

const grds = ['linear-gradient(135deg,#fbcfe8,#fce7f3)','linear-gradient(135deg,#bae6fd,#bfdbfe)','linear-gradient(135deg,#fde68a,#fed7aa)','linear-gradient(135deg,#c4b5fd,#a5b4fc)','linear-gradient(135deg,#a7f3d0,#bef264)','linear-gradient(135deg,#fda4af,#fbbf24)']
function gradient(id) { return grds[(id || 0) % grds.length] }
const emojis = ['🎮','💡','🎵','📚','🎯','🌟','💻','🎨','🚀','🌈']
function emoji(id) { return emojis[(id || 0) % emojis.length] }
const avColors = ['#6366f1','#06b6d4','#f59e0b','#8b5cf6','#10b981','#ef4444','#ec4899','#14b8a6']
function avColor(id) { return avColors[(id || 0) % avColors.length] }

function formatTime(t) {
  if (!t) return ''
  return new Date(t).toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
}
function formatDay(t) {
  const d = new Date(t)
  const today = new Date()
  const yesterday = new Date(today.getTime() - 86400000)
  if (d.toDateString() === today.toDateString()) return '今天 ' + formatTime(t)
  if (d.toDateString() === yesterday.toDateString()) return '昨天 ' + formatTime(t)
  return d.toLocaleDateString('zh-CN') + ' ' + formatTime(t)
}
// 距离上一条消息超过 5 分钟则显示日期分隔
function shouldShowTime(msg, prev) {
  if (!prev) return true
  const diff = new Date(msg.createdAt) - new Date(prev.createdAt)
  return diff > 5 * 60 * 1000
}
// 同一发送者的连续消息隐藏头像和昵称
function isContinuation(msg, prev) {
  if (!prev || prev.messageType === 'System' || msg.messageType === 'System') return false
  if (prev.senderId !== msg.senderId) return false
  const diff = new Date(msg.createdAt) - new Date(prev.createdAt)
  return diff < 5 * 60 * 1000
}

async function loadGroup() {
  try {
    const res = await getGroupDetail(groupId.value)
    groupInfo.value = res.data
  } catch { router.push('/lobby') }
}

function autoResize() {
  if (!inputBox.value) return
  inputBox.value.style.height = 'auto'
  inputBox.value.style.height = Math.min(inputBox.value.scrollHeight, 120) + 'px'
}
function onKeyDown(e) {
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    handleSend()
  }
}
async function handleSend() {
  const text = inputText.value.trim()
  if (!text || isMuted.value) return
  await chatStore.sendMessage(groupId.value, text)
  inputText.value = ''
  nextTick(() => { autoResize(); scrollBottom() })
}
async function handleFile(e) {
  const file = e.target.files?.[0]
  if (!file) return
  uploading.value = true
  try {
    const res = await uploadFile(file)
    const isImage = /\.(jpg|jpeg|png|gif|webp|bmp)$/i.test(file.name)
    await chatStore.sendMessage(groupId.value, file.name, isImage ? 1 : 2, res.data.fileUrl)
    nextTick(() => scrollBottom())
  } catch { toast.error('上传失败') }
  uploading.value = false
  e.target.value = ''
}
function previewImage(url) { previewUrl.value = url }
function startPM(uid) { router.push(`/chat/new?userId=${uid}`) }

async function acceptPm() {
  const req = chatStore.privateChatRequest
  if (!req) return
  await chatStore.acceptPrivateChat(req.chatId, true)
  showPmRequest.value = false
  chatStore.privateChatRequest = null
  router.push(`/chat/${req.chatId}`)
}
async function rejectPm() {
  const req = chatStore.privateChatRequest
  if (req) await chatStore.acceptPrivateChat(req.chatId, false)
  showPmRequest.value = false
  chatStore.privateChatRequest = null
}

// 收到私聊请求时弹窗
watch(() => chatStore.privateChatRequest, (req) => {
  if (req) showPmRequest.value = true
})

async function handleMute(uid, mute) {
  await toggleMute(groupId.value, uid, mute)
  toast.success(mute ? '已禁言' : '已解禁')
  loadGroup()
}
async function handleKick(uid) {
  const ok = await confirm('踢出成员', '确定将此成员踢出群聊？')
  if (!ok) return
  await kickMember(groupId.value, uid)
  toast.success('已踢出')
  loadGroup()
}
async function handleQuit() {
  const ok = await confirm('退出群聊', '确定要退出该群聊吗？')
  if (!ok) return
  await leaveGroup(groupId.value)
  await chatStore.exitGroup(groupId.value)
  await chatStore.loadMyGroups()
  toast.success('已退出群聊')
  router.push('/lobby')
}
async function handleDisband() {
  const ok = await confirm('解散群聊', '此操作不可撤销，确定解散该群？')
  if (!ok) return
  await dissolveGroup(groupId.value)
  await chatStore.exitGroup(groupId.value)
  await chatStore.loadMyGroups()
  toast.success('群聊已解散')
  router.push('/lobby')
}

watch(showSettings, (v) => {
  if (v && groupInfo.value) {
    settingsForm.value = {
      name: groupInfo.value.name || '',
      description: groupInfo.value.description || '',
      password: '', question: '', questionAnswer: ''
    }
  }
})
async function saveSettings() {
  savingSettings.value = true
  try {
    await updateGroup(groupId.value, {
      name: settingsForm.value.name || null,
      description: settingsForm.value.description || null,
      password: settingsForm.value.password || null,
      question: settingsForm.value.question || null,
      questionAnswer: settingsForm.value.questionAnswer || null
    })
    toast.success('已保存')
    showSettings.value = false
    loadGroup()
    chatStore.loadMyGroups()
  } catch { toast.error('保存失败') }
  savingSettings.value = false
}

// 滚动控制
function onScroll() {
  if (!msgArea.value) return
  const { scrollTop, scrollHeight, clientHeight } = msgArea.value
  atBottom.value = scrollHeight - scrollTop - clientHeight < 80
}
function scrollBottom() {
  msgEnd.value?.scrollIntoView({ behavior: 'smooth' })
}
let pageNum = 1
async function loadMore() {
  if (chatStore.loadingHistory || !chatStore.hasMoreHistory) return
  pageNum++
  const oldHeight = msgArea.value?.scrollHeight
  await chatStore.loadGroupHistory(groupId.value, pageNum)
  nextTick(() => {
    if (msgArea.value) {
      msgArea.value.scrollTop = msgArea.value.scrollHeight - oldHeight
    }
  })
}

// 切换群
async function switchGroup(id) {
  if (id === groupId.value) return
  await chatStore.exitGroup(groupId.value)
  router.push(`/group/${id}`)
}

// 新消息时若在底部则自动滚动
watch(() => chatStore.groupMessages.length, () => {
  if (atBottom.value) nextTick(() => scrollBottom())
})

// 路由切换时重新加载
watch(groupId, async (newId, oldId) => {
  if (oldId) await chatStore.exitGroup(oldId)
  pageNum = 1
  groupInfo.value = null
  await loadGroup()
  await chatStore.enterGroup(newId)
  nextTick(() => scrollBottom())
})

onMounted(async () => {
  await chatStore.connect()   // 确保 SignalR 已连接（幂等，已连接则跳过）
  await chatStore.loadMyGroups()
  await loadGroup()
  await chatStore.enterGroup(groupId.value)
  nextTick(() => scrollBottom())
})
onBeforeUnmount(() => { chatStore.exitGroup(groupId.value) })
</script>

<style scoped>
.chat-page {
  display: flex;
  height: 100vh;
  padding: 12px;
  gap: 12px;
}

/* 侧边栏 */
.sidebar {
  width: 240px;
  flex-shrink: 0;
  border-radius: var(--r-lg);
  display: flex; flex-direction: column;
  overflow: hidden;
}
.sidebar-head { padding: 10px; border-bottom: 1px solid var(--border-soft); }
.sidebar-title {
  padding: 12px 14px 6px;
  font-size: 11px;
  color: var(--text-tertiary);
  font-weight: 600;
  letter-spacing: .5px;
  text-transform: uppercase;
}
.group-list { flex: 1; overflow-y: auto; padding: 4px 6px 8px; }
.group-item {
  display: flex; align-items: center; gap: 10px;
  padding: 10px 10px;
  border-radius: var(--r-md);
  cursor: pointer;
  transition: background .15s;
  margin-bottom: 2px;
}
.group-item:hover { background: rgba(0,0,0,.04); }
.group-item.active {
  background: var(--accent-gradient);
  color: #fff;
  box-shadow: 0 6px 16px rgba(99,102,241,.3);
}
.group-item.active .g-meta { color: rgba(255,255,255,.8); }
.g-avatar {
  width: 40px; height: 40px;
  border-radius: var(--r-sm);
  display: flex; align-items: center; justify-content: center;
  font-size: 20px;
  flex-shrink: 0;
}
.g-info { min-width: 0; flex: 1; }
.g-name { font-weight: 500; font-size: 14px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.g-meta { font-size: 12px; color: var(--text-tertiary); margin-top: 2px; }
.sidebar-empty {
  text-align: center;
  padding: 40px 16px;
  color: var(--text-tertiary);
  font-size: 13px;
}
.back-btn { width: 100%; justify-content: flex-start; }

/* 主聊天区 */
.chat-main {
  flex: 1;
  display: flex; flex-direction: column;
  min-width: 0;
  border-radius: var(--r-lg);
  overflow: hidden;
}
.chat-head {
  display: flex; align-items: center; justify-content: space-between;
  padding: 16px 24px;
  border-radius: var(--r-lg) var(--r-lg) 0 0;
}
.head-info { display: flex; align-items: center; gap: 14px; }
.head-icon {
  width: 46px; height: 46px;
  border-radius: var(--r-sm);
  display: flex; align-items: center; justify-content: center;
  font-size: 24px;
}
.head-name { font-size: 17px; font-weight: 600; }
.card-id { font-weight: 400; color: var(--text-tertiary); font-size: 13px; margin-left: 4px; }
.head-sub { font-size: 13px; color: var(--text-tertiary); margin-top: 2px; }
.head-actions { display: flex; align-items: center; gap: 6px; }
.btn-icon.active { background: rgba(99,102,241,.15); color: var(--accent); }

/* 消息区域 */
.messages-area {
  flex: 1;
  overflow-y: auto;
  padding: 24px 32px;
  position: relative;
  background: rgba(255,255,255,.4);
  backdrop-filter: blur(20px);
}
.msg-list { max-width: 100%; }
.load-more {
  text-align: center;
  padding: 12px;
  font-size: 12px;
  color: var(--text-tertiary);
}
.load-more.clickable { cursor: pointer; transition: color .15s; }
.load-more.clickable:hover { color: var(--accent); }

.time-divider {
  text-align: center;
  margin: 20px 0 12px;
  position: relative;
}
.time-divider::before {
  content: '';
  position: absolute; top: 50%; left: 0; right: 0;
  height: 1px; background: rgba(0,0,0,.06);
}
.time-divider span {
  position: relative;
  padding: 4px 12px;
  background: rgba(255,255,255,.7);
  border-radius: var(--r-full);
  font-size: 11px;
  color: var(--text-tertiary);
  display: inline-block;
}

.sys-msg {
  text-align: center;
  margin: 12px 0;
  padding: 4px 14px;
  background: rgba(255,255,255,.7);
  border-radius: var(--r-full);
  font-size: 11px;
  color: var(--text-tertiary);
  display: inline-block;
  width: 100%;
  max-width: max-content;
  margin-left: auto;
  margin-right: auto;
}

.msg-row {
  display: flex; gap: 10px;
  margin-bottom: 4px;
  position: relative;
  align-items: flex-end;
}
.msg-row.mine { flex-direction: row-reverse; }
.msg-avatar {
  width: 42px; height: 42px;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  color: #fff;
  font-weight: 600;
  font-size: 16px;
  flex-shrink: 0;
}
.msg-row.hideAvatar { margin-bottom: 2px; }
.msg-row.hideAvatar .msg-avatar { visibility: hidden; height: 0; }

.msg-body {
  max-width: 68%;
  display: flex; flex-direction: column;
  gap: 4px;
}
.msg-row.mine .msg-body { align-items: flex-end; }
.msg-meta {
  display: flex; gap: 8px; align-items: baseline;
  margin-bottom: 2px;
  padding: 0 4px;
}
.msg-row.mine .msg-meta { display: none; }
.msg-name { font-size: 13px; color: var(--accent); font-weight: 600; }
.msg-time { font-size: 12px; color: var(--text-tertiary); }

.msg-bubble {
  padding: 12px 16px;
  background: var(--bubble-other);
  border-radius: var(--r-lg);
  font-size: 15px;
  line-height: 1.6;
  word-break: break-word;
  box-shadow: 0 1px 2px rgba(0,0,0,.04);
  max-width: 100%;
}
.msg-bubble.mine {
  background: var(--bubble-mine);
  color: #fff;
  box-shadow: 0 4px 12px rgba(99,102,241,.25);
}
.msg-image {
  max-width: 300px;
  max-height: 300px;
  border-radius: var(--r-sm);
  cursor: pointer;
  display: block;
}
.msg-file { color: inherit; text-decoration: underline; }

.msg-status {
  font-size: 10px; color: var(--text-tertiary);
  padding: 0 4px;
}
.status-failed { color: #dc2626; }

.pm-btn {
  align-self: center;
  background: transparent;
  border: 1px solid var(--border-soft);
  border-radius: var(--r-full);
  padding: 4px 10px;
  font-size: 11px;
  color: var(--text-secondary);
  cursor: pointer;
  opacity: 0;
  transition: opacity .2s;
}
.msg-row:hover .pm-btn { opacity: 1; }
.pm-btn:hover { background: var(--accent); color: #fff; border-color: transparent; }

.to-bottom-btn {
  position: sticky;
  bottom: 16px;
  margin-left: auto;
  margin-right: 16px;
  width: 36px; height: 36px;
  border-radius: 50%;
  background: var(--bg-glass-strong);
  backdrop-filter: blur(10px);
  border: 1px solid var(--border);
  color: var(--text-primary);
  cursor: pointer;
  box-shadow: var(--shadow-md);
  display: block;
}

/* 输入框 */
.input-area {
  display: flex; align-items: flex-end; gap: 10px;
  padding: 16px 20px;
  border-radius: 0 0 var(--r-lg) var(--r-lg);
}
.msg-input {
  flex: 1;
  padding: 12px 16px;
  background: rgba(255,255,255,.7);
  border: 1px solid transparent;
  border-radius: var(--r-md);
  font-size: 15px;
  color: var(--text-primary);
  outline: none;
  resize: none;
  max-height: 120px;
  font-family: inherit;
  line-height: 1.5;
  transition: all .2s;
}
.msg-input:focus {
  border-color: var(--accent-light);
  background: rgba(255,255,255,.95);
  box-shadow: 0 0 0 3px rgba(99,102,241,.1);
}
.send-btn { padding: 12px 24px; align-self: flex-end; font-size: 14px; }

/* 成员面板 */
.members-panel {
  width: 260px;
  flex-shrink: 0;
  border-radius: var(--r-lg);
  display: flex; flex-direction: column;
  overflow: hidden;
}
.panel-head {
  display: flex; align-items: center; justify-content: space-between;
  padding: 14px 16px;
  font-size: 13px;
  font-weight: 600;
  border-bottom: 1px solid var(--border-soft);
}
.member-list { flex: 1; overflow-y: auto; padding: 8px; }
.member-item {
  display: flex; align-items: center; gap: 10px;
  padding: 8px 10px;
  border-radius: var(--r-md);
  transition: background .15s;
}
.member-item:hover { background: rgba(0,0,0,.03); }
.m-avatar {
  width: 32px; height: 32px;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  color: #fff;
  font-weight: 600;
  font-size: 13px;
  flex-shrink: 0;
}
.m-info { flex: 1; min-width: 0; }
.m-name {
  font-size: 13px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.m-tag {
  font-size: 10px;
  padding: 1px 6px;
  border-radius: var(--r-full);
  font-weight: 500;
}
.m-tag.owner { background: rgba(245,158,11,.15); color: #d97706; }
.m-tag.admin { background: rgba(99,102,241,.15); color: var(--accent); }
.m-tag.muted { background: rgba(239,68,68,.15); color: #dc2626; }
.m-actions { display: flex; gap: 2px; }
.btn-icon.small { width: 28px; height: 28px; font-size: 12px; }

/* 模态框 */
.modal-form { display: flex; flex-direction: column; gap: 14px; }
.field { display: flex; flex-direction: column; gap: 6px; }
.field label { font-size: 12px; color: var(--text-secondary); font-weight: 500; }

/* 图片预览 */
.img-preview {
  position: fixed; inset: 0;
  background: rgba(0,0,0,.85);
  display: flex; align-items: center; justify-content: center;
  z-index: 200;
  cursor: zoom-out;
}
.img-preview img {
  max-width: 90vw; max-height: 90vh;
  border-radius: var(--r-md);
}

/* 私聊请求弹窗头像 */
.pm-req-avatar {
  width: 64px; height: 64px;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 26px; font-weight: 700; color: #fff;
  margin: 0 auto;
  box-shadow: 0 4px 12px rgba(0,0,0,.12);
}

/* ========== 响应式 ========== */

/* 侧边栏遮罩（手机端） */
.sidebar-mask {
  display: none;
  position: fixed; inset: 0;
  background: rgba(0,0,0,.3);
  backdrop-filter: blur(2px);
  z-index: 40;
}
/* 汉堡按钮默认隐藏（桌面端不需要） */
.sidebar-toggle { display: none; }

/* 平板：侧边栏收窄 */
@media (max-width: 1024px) {
  .sidebar { width: 200px; }
  .g-name { font-size: 13px; }
  .g-meta { font-size: 11px; }
  .messages-area { padding: 16px 20px; }
  .msg-body { max-width: 75%; }
}

/* 手机：侧边栏抽屉式 */
@media (max-width: 640px) {
  .chat-page { padding: 0; gap: 0; }

  .sidebar {
    position: fixed;
    left: 0; top: 0; bottom: 0;
    width: 280px;
    z-index: 50;
    border-radius: 0 var(--r-lg) var(--r-lg) 0;
    transform: translateX(-100%);
    transition: transform .25s ease;
  }
  .sidebar.sidebar-open { transform: translateX(0); }
  .sidebar-mask { display: block; }

  .chat-main { border-radius: 0; }
  .chat-head { border-radius: 0; padding: 12px 14px; }
  .sidebar-toggle { display: flex; }

  .head-icon { width: 36px; height: 36px; font-size: 18px; }
  .head-name { font-size: 15px; }
  .head-sub { font-size: 11px; }

  .messages-area { padding: 12px 14px; }
  .msg-body { max-width: 82%; }
  .msg-bubble { font-size: 14px; padding: 10px 13px; }
  .msg-avatar { width: 34px; height: 34px; font-size: 13px; }

  .input-area { padding: 10px 12px; border-radius: 0; }
  .msg-input { font-size: 14px; }
  .send-btn { padding: 10px 16px; font-size: 13px; }

  .members-panel {
    position: fixed;
    right: 0; top: 0; bottom: 0;
    width: 260px;
    z-index: 50;
    border-radius: var(--r-lg) 0 0 var(--r-lg);
  }
}
</style>
