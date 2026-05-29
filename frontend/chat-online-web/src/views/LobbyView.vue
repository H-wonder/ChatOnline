<template>
  <div class="lobby-page">
    <!-- 顶部导航 -->
    <header class="topbar glass-strong">
      <div class="topbar-left">
        <div class="brand">
          <div class="brand-icon">💬</div>
          <span class="brand-name">ChatOnline</span>
        </div>
      </div>
      <div class="topbar-search">
        <span class="search-icon">🔍</span>
        <input class="search-input" v-model="search" placeholder="搜索群聊名称..." @input="loadGroups" />
      </div>
      <div class="topbar-right">
        <button class="btn btn-ghost" @click="showIdJoin = true">🔗 输入群号</button>
        <button class="btn btn-primary" @click="$router.push('/create-group')">+ 创建群聊</button>
        <div class="user-menu">
          <div class="user-avatar" @click="userMenuOpen = !userMenuOpen">
            {{ authStore.user?.username?.[0]?.toUpperCase() }}
          </div>
          <div v-if="userMenuOpen" class="user-dropdown glass-strong" @click="userMenuOpen = false">
            <div class="dd-header">
              <div class="dd-avatar">{{ authStore.user?.username?.[0]?.toUpperCase() }}</div>
              <div class="dd-info">
                <div class="dd-name">{{ authStore.user?.username }}</div>
                <div class="dd-bio">{{ authStore.user?.bio || '点击编辑资料' }}</div>
              </div>
            </div>
            <button class="dd-item" @click="openProfile">编辑个人资料</button>
            <button class="dd-item dd-danger" @click="handleLogout">退出登录</button>
          </div>
        </div>
      </div>
    </header>

    <!-- 主内容 -->
    <main class="main">
      <!-- 我加入的群（横向） -->
      <section v-if="chatStore.myGroups.length" class="my-groups-section">
        <div class="section-header">
          <h2>我的群聊</h2>
          <span class="section-count">{{ chatStore.myGroups.length }}</span>
        </div>
        <div class="my-groups-row">
          <div v-for="g in chatStore.myGroups" :key="g.id"
            class="my-group-pill glass" @click="$router.push(`/group/${g.id}`)">
            <div class="pill-icon" :style="{ background: gradient(g.id) }">{{ emoji(g.id) }}</div>
            <div class="pill-info">
              <div class="pill-name">{{ g.name }}</div>
              <div class="pill-count">{{ g.memberCount }} 人</div>
            </div>
          </div>
        </div>
      </section>

      <!-- 大厅 -->
      <section class="lobby-section">
        <div class="section-header">
          <h2>群聊大厅</h2>
          <span class="section-count" v-if="groups.length">{{ groups.length }}</span>
        </div>

        <div v-if="groups.length" class="group-grid">
          <article v-for="g in groups" :key="g.id" class="group-card glass" @click="handleJoin(g)">
            <div class="card-cover" :style="{ background: gradient(g.id) }">
              <span class="card-emoji">{{ emoji(g.id) }}</span>
            </div>
            <div class="card-body">
              <div class="card-title">
                <h3>{{ g.name }}</h3>
                <span class="card-id">#{{ g.id }}</span>
              </div>
              <p class="card-desc">{{ g.description || '暂无简介' }}</p>
              <div class="card-meta">
                <span>👥 {{ g.memberCount }}</span>
                <span v-if="g.hasPassword" title="有密码">🔒</span>
                <span v-if="g.hasQuestion" title="有问答">❓</span>
              </div>
            </div>
          </article>
        </div>

        <div v-else class="empty">
          <div class="empty-icon">🌱</div>
          <h3>还没有群聊</h3>
          <p>来创建第一个群聊吧</p>
        </div>
      </section>
    </main>

    <!-- 加入弹窗 -->
    <Modal v-if="dialogVisible" :title="`加入 ${joiningGroup?.name}`" @close="dialogVisible = false">
      <div class="modal-form">
        <div class="modal-preview">
          <div class="preview-icon" :style="{ background: gradient(joiningGroup?.id) }">{{ emoji(joiningGroup?.id) }}</div>
          <div>
            <div class="preview-name">{{ joiningGroup?.name }} <span class="card-id">#{{ joiningGroup?.id }}</span></div>
            <div class="preview-desc">{{ joiningGroup?.description || '暂无简介' }}</div>
          </div>
        </div>
        <div class="field"><label>你的匿名昵称</label>
          <input class="input" v-model="joinForm.anonNickname" placeholder="群里其他人看到这个名字" maxlength="20" />
        </div>
        <div class="field" v-if="joiningGroup?.hasPassword"><label>入群密码</label>
          <input class="input" type="password" v-model="joinForm.password" placeholder="请输入密码" />
        </div>
        <div class="field" v-if="joiningGroup?.hasQuestion"><label>入群答案</label>
          <input class="input" v-model="joinForm.answer" placeholder="请输入答案" />
        </div>
      </div>
      <template #footer>
        <button class="btn btn-ghost" @click="dialogVisible = false">取消</button>
        <button class="btn btn-primary" @click="confirmJoin" :disabled="joining">{{ joining ? '加入中...' : '确认加入' }}</button>
      </template>
    </Modal>

    <!-- 编辑资料弹窗 -->
    <Modal v-if="showProfile" title="编辑个人资料" @close="showProfile = false">
      <div class="modal-form">
        <div class="field"><label>头像图片链接</label>
          <input class="input" v-model="profileForm.avatar" placeholder="https://... （选填）" />
        </div>
        <div class="field"><label>个人简介</label>
          <textarea class="input" v-model="profileForm.bio" placeholder="介绍一下自己" rows="3" maxlength="200"></textarea>
        </div>
      </div>
      <template #footer>
        <button class="btn btn-ghost" @click="showProfile = false">取消</button>
        <button class="btn btn-primary" @click="handleSaveProfile" :disabled="savingProfile">保存</button>
      </template>
    </Modal>

    <!-- 群号加入弹窗 -->
    <Modal v-if="showIdJoin" title="通过群号加入" @close="showIdJoin = false">
      <div class="modal-form">
        <div class="field"><label>群号</label>
          <input class="input" type="number" v-model.number="idJoinForm.groupId" placeholder="例如：1" />
        </div>
        <div class="field"><label>匿名昵称</label>
          <input class="input" v-model="idJoinForm.anonNickname" placeholder="群内显示的名字" maxlength="20" />
        </div>
      </div>
      <template #footer>
        <button class="btn btn-ghost" @click="showIdJoin = false">取消</button>
        <button class="btn btn-primary" @click="handleIdJoin" :disabled="idJoining">{{ idJoining ? '加入中...' : '加入' }}</button>
      </template>
    </Modal>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/authStore'
import { useChatStore } from '@/stores/chatStore'
import { getGroups, joinGroup } from '@/api/groupApi'
import { updateProfile } from '@/api/messageApi'
import { toast } from '@/utils/toast.js'
import Modal from '@/components/Modal.vue'

const router = useRouter()
const authStore = useAuthStore()
const chatStore = useChatStore()

const search = ref('')
const groups = ref([])
const dialogVisible = ref(false)
const joining = ref(false)
const joiningGroup = ref(null)
const joinForm = ref({ anonNickname: '', password: '', answer: '' })
const showProfile = ref(false)
const savingProfile = ref(false)
const profileForm = ref({ avatar: '', bio: '' })
const showIdJoin = ref(false)
const idJoining = ref(false)
const idJoinForm = ref({ groupId: null, anonNickname: '' })
const userMenuOpen = ref(false)

const grds = [
  'linear-gradient(135deg,#fbcfe8,#fce7f3)',
  'linear-gradient(135deg,#bae6fd,#bfdbfe)',
  'linear-gradient(135deg,#fde68a,#fed7aa)',
  'linear-gradient(135deg,#c4b5fd,#a5b4fc)',
  'linear-gradient(135deg,#a7f3d0,#bef264)',
  'linear-gradient(135deg,#fda4af,#fbbf24)'
]
function gradient(id) { return grds[(id || 0) % grds.length] }
const emojis = ['🎮','💡','🎵','📚','🎯','🌟','💻','🎨','🚀','🌈']
function emoji(id) { return emojis[(id || 0) % emojis.length] }

async function loadGroups() {
  const res = await getGroups(search.value || null)
  groups.value = res.data || []
}

function handleJoin(group) {
  joiningGroup.value = group
  joinForm.value = { anonNickname: '路人' + Math.random().toString(36).slice(2,6), password: '', answer: '' }
  dialogVisible.value = true
}

async function confirmJoin() {
  if (!joinForm.value.anonNickname.trim()) { toast.warning('请输入匿名昵称'); return }
  joining.value = true
  try {
    const res = await joinGroup(joiningGroup.value.id, {
      anonNickname: joinForm.value.anonNickname,
      password: joinForm.value.password || null,
      answer: joinForm.value.answer || null
    })
    toast.success(res.data.message)
    dialogVisible.value = false
    router.push(`/group/${joiningGroup.value.id}`)
  } catch (err) {
    const msg = err?.response?.data?.message || ''
    if (msg === '你已在该群中') {
      dialogVisible.value = false
      router.push(`/group/${joiningGroup.value.id}`)
    } else toast.error(msg || '加入失败')
  }
  joining.value = false
}

function openProfile() {
  profileForm.value = {
    avatar: authStore.user?.avatar || '',
    bio: authStore.user?.bio || ''
  }
  showProfile.value = true
}

async function handleSaveProfile() {
  savingProfile.value = true
  try {
    await updateProfile({ avatar: profileForm.value.avatar || null, bio: profileForm.value.bio || null })
    const u = { ...authStore.user, avatar: profileForm.value.avatar, bio: profileForm.value.bio }
    authStore.user = u
    localStorage.setItem('user', JSON.stringify(u))
    toast.success('已保存')
    showProfile.value = false
  } catch { toast.error('保存失败') }
  savingProfile.value = false
}

async function handleIdJoin() {
  if (!idJoinForm.value.groupId || !idJoinForm.value.anonNickname?.trim()) {
    toast.warning('请输入群号和昵称'); return
  }
  idJoining.value = true
  try {
    const res = await joinGroup(idJoinForm.value.groupId, {
      anonNickname: idJoinForm.value.anonNickname, password: null, answer: null
    })
    toast.success(res.data.message)
    showIdJoin.value = false
    router.push(`/group/${idJoinForm.value.groupId}`)
  } catch (err) {
    const msg = err?.response?.data?.message || ''
    if (msg === '你已在该群中') {
      showIdJoin.value = false
      router.push(`/group/${idJoinForm.value.groupId}`)
    } else toast.error(msg || '加入失败')
  }
  idJoining.value = false
}

function handleLogout() { authStore.logout(); router.push('/login') }

function onClickOutside(e) {
  if (!e.target.closest('.user-menu')) userMenuOpen.value = false
}

onMounted(() => {
  loadGroups()
  chatStore.loadMyGroups()
  chatStore.connect()
  document.addEventListener('click', onClickOutside)
})
onBeforeUnmount(() => document.removeEventListener('click', onClickOutside))
</script>

<style scoped>
.lobby-page { min-height: 100vh; padding-bottom: 40px; }

/* 顶部导航 */
.topbar {
  position: sticky; top: 0; z-index: 30;
  display: flex; align-items: center; gap: 16px;
  padding: 16px 28px;
  margin: 16px 24px 0;
  border-radius: var(--r-lg);
}
.brand { display: flex; align-items: center; gap: 12px; }
.brand-icon {
  width: 44px; height: 44px;
  background: var(--accent-gradient);
  border-radius: var(--r-sm);
  display: flex; align-items: center; justify-content: center;
  font-size: 24px;
  box-shadow: 0 4px 12px rgba(99,102,241,.3);
}
.brand-name {
  font-size: 20px; font-weight: 700;
  background: var(--accent-gradient);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}
.topbar-search {
  flex: 1; max-width: 480px;
  position: relative;
  display: flex; align-items: center;
}
.search-icon {
  position: absolute; left: 16px;
  pointer-events: none; opacity: .5; font-size: 16px;
}
.search-input {
  width: 100%;
  padding: 12px 16px 12px 44px;
  background: rgba(255,255,255,.5);
  border: 1px solid transparent;
  border-radius: var(--r-full);
  font-size: 14px;
  outline: none;
  transition: all .2s;
}
.search-input:focus {
  background: rgba(255,255,255,.95);
  border-color: var(--accent-light);
}
.topbar-right { display: flex; align-items: center; gap: 12px; margin-left: auto; }

/* 用户头像 + 下拉菜单 */
.user-menu { position: relative; }
.user-avatar {
  width: 44px; height: 44px;
  border-radius: 50%;
  background: var(--accent-gradient);
  color: #fff;
  display: flex; align-items: center; justify-content: center;
  font-weight: 600; font-size: 16px;
  cursor: pointer;
  box-shadow: 0 4px 12px rgba(99,102,241,.3);
  transition: transform .2s;
}
.user-avatar:hover { transform: scale(1.06); }
.user-dropdown {
  position: absolute; top: calc(100% + 8px); right: 0;
  width: 240px;
  border-radius: var(--r-md);
  padding: 12px;
  box-shadow: var(--shadow-lg);
  animation: fadeIn .2s ease;
}
.dd-header {
  display: flex; gap: 12px; align-items: center;
  padding: 8px 8px 12px;
  border-bottom: 1px solid var(--border-soft);
  margin-bottom: 8px;
}
.dd-avatar {
  width: 40px; height: 40px;
  border-radius: 50%;
  background: var(--accent-gradient);
  color: #fff;
  display: flex; align-items: center; justify-content: center;
  font-weight: 600;
}
.dd-name { font-weight: 600; font-size: 14px; }
.dd-bio { font-size: 12px; color: var(--text-tertiary); margin-top: 2px; }
.dd-item {
  display: block; width: 100%;
  padding: 10px 12px;
  background: transparent; border: none;
  text-align: left;
  border-radius: var(--r-sm);
  font-size: 13px;
  color: var(--text-primary);
  cursor: pointer;
  transition: background .15s;
}
.dd-item:hover { background: rgba(0,0,0,.04); }
.dd-danger { color: #dc2626; }
.dd-danger:hover { background: rgba(239,68,68,.08); }

/* 主区域 */
.main { max-width: 1400px; margin: 0 auto; padding: 28px 32px; }

.section-header {
  display: flex; align-items: baseline; gap: 12px;
  margin-bottom: 20px;
}
.section-header h2 { margin: 0; font-size: 20px; font-weight: 700; }
.section-count {
  font-size: 13px;
  color: var(--text-tertiary);
  background: rgba(0,0,0,.06);
  padding: 2px 10px;
  border-radius: var(--r-full);
}

/* 我加入的群（横向滚动） */
.my-groups-section { margin-bottom: 36px; }
.my-groups-row {
  display: flex; gap: 14px;
  overflow-x: auto;
  padding-bottom: 6px;
  scroll-snap-type: x mandatory;
}
.my-group-pill {
  display: flex; align-items: center; gap: 12px;
  padding: 14px 20px;
  border-radius: var(--r-md);
  cursor: pointer;
  flex-shrink: 0;
  scroll-snap-align: start;
  transition: all .2s;
  min-width: 200px;
}
.my-group-pill:hover { transform: translateY(-2px); box-shadow: var(--shadow-md); }
.pill-icon {
  width: 48px; height: 48px;
  border-radius: var(--r-sm);
  display: flex; align-items: center; justify-content: center;
  font-size: 24px;
  flex-shrink: 0;
}
.pill-name { font-weight: 600; font-size: 15px; }
.pill-count { font-size: 12px; color: var(--text-tertiary); margin-top: 3px; }

/* 群卡片网格 */
.group-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 20px;
}
.group-card {
  border-radius: var(--r-lg);
  overflow: hidden;
  cursor: pointer;
  transition: all .25s ease;
}
.group-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
}
.card-cover {
  height: 130px;
  display: flex; align-items: center; justify-content: center;
  position: relative;
}
.card-emoji { font-size: 52px; filter: drop-shadow(0 2px 8px rgba(0,0,0,.1)); }
.card-body { padding: 18px 20px; }
.card-title { display: flex; align-items: baseline; gap: 6px; margin-bottom: 8px; }
.card-title h3 {
  margin: 0;
  font-size: 17px;
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
}
.card-id {
  font-size: 12px;
  color: var(--text-tertiary);
  font-weight: 400;
}
.card-desc {
  margin: 0 0 12px;
  font-size: 13px;
  color: var(--text-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  min-height: 20px;
}
.card-meta {
  display: flex; gap: 12px;
  font-size: 13px;
  color: var(--text-tertiary);
}

/* Empty state */
.empty {
  text-align: center;
  padding: 80px 0;
}
.empty-icon { font-size: 64px; margin-bottom: 16px; }
.empty h3 { margin: 0 0 6px; color: var(--text-primary); }
.empty p { margin: 0; color: var(--text-secondary); font-size: 13px; }

/* Modal 内表单 */
.modal-form { display: flex; flex-direction: column; gap: 16px; }
.field { display: flex; flex-direction: column; gap: 6px; }
.field label { font-size: 12px; color: var(--text-secondary); font-weight: 500; margin-left: 4px; }
.modal-preview {
  display: flex; gap: 12px; align-items: center;
  padding: 12px;
  background: rgba(0,0,0,.03);
  border-radius: var(--r-md);
}
.preview-icon {
  width: 48px; height: 48px;
  border-radius: var(--r-sm);
  display: flex; align-items: center; justify-content: center;
  font-size: 24px;
  flex-shrink: 0;
}
.preview-name { font-size: 14px; font-weight: 600; }
.preview-desc { font-size: 12px; color: var(--text-tertiary); margin-top: 2px; }

/* ========== 响应式 ========== */

/* 平板 */
@media (max-width: 1024px) {
  .topbar { margin: 12px 16px 0; padding: 14px 20px; }
  .main { padding: 20px 16px; }
  .group-grid { grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 16px; }
  .topbar-search { max-width: 320px; }
}

/* 手机 */
@media (max-width: 640px) {
  .topbar {
    margin: 8px 10px 0;
    padding: 10px 14px;
    flex-wrap: wrap;
    gap: 10px;
  }
  .brand-name { font-size: 16px; }
  .brand-icon { width: 36px; height: 36px; font-size: 20px; }

  /* 搜索框独占一行 */
  .topbar-search {
    order: 3;
    flex: 0 0 100%;
    max-width: 100%;
  }
  .topbar-right { margin-left: auto; }

  /* 隐藏"输入群号"文字，只保留图标 */
  .topbar-right .btn-ghost { display: none; }

  .main { padding: 16px 10px; }
  .section-header h2 { font-size: 17px; }

  .group-grid { grid-template-columns: 1fr 1fr; gap: 12px; }
  .card-cover { height: 90px; }
  .card-emoji { font-size: 36px; }
  .card-body { padding: 12px 14px; }
  .card-title h3 { font-size: 14px; }
  .card-desc { font-size: 12px; }

  .my-group-pill { min-width: 160px; padding: 10px 14px; }
  .pill-icon { width: 40px; height: 40px; font-size: 20px; }
  .pill-name { font-size: 13px; }

  .user-avatar { width: 38px; height: 38px; font-size: 14px; }
}

/* 超小屏 */
@media (max-width: 400px) {
  .group-grid { grid-template-columns: 1fr; }
}
</style>
