import { defineStore } from 'pinia'
import { ref } from 'vue'
import { getGroupMessages, getPrivateMessages } from '@/api/messageApi'
import { getMyGroups } from '@/api/groupApi'
import {
  startConnection, stopConnection, getConnection,
  onGroupMessage, onUserJoined, onUserLeft,
  onPrivateChatRequest, onPrivateChatAccepted, onPrivateChatRejected,
  onPrivateMessage, onIdentityRevealed,
  onMemberKicked, onMemberMuted, onGroupDissolved, onError,
  joinGroup, leaveGroup, sendGroupMessage,
  requestPrivateChat, respondPrivateChat, sendPrivateMessage, revealIdentity
} from '@/api/signalRClient'

export const useChatStore = defineStore('chat', () => {
  const connected = ref(false)
  const groupMessages = ref([])        // 当前群的消息
  const privateMessages = ref([])      // 当前私聊的消息
  const privateChatId = ref(null)      // 当前私聊 ID
  const privateChatRequest = ref(null) // 收到的私聊请求
  const myGroups = ref([])             // 我加入的群（侧边栏用）
  const loadingHistory = ref(false)    // 加载历史中
  const hasMoreHistory = ref(true)     // 是否还有更多历史

  // 建立 SignalR 连接
  async function connect() {
    if (connected.value) return
    await startConnection()
    connected.value = true
    registerEvents()
  }

  // 断开连接
  async function disconnect() {
    await stopConnection()
    connected.value = false
  }

  // 注册所有服务端推送事件
  function registerEvents() {
    onGroupMessage(msg => {
      // 用服务端真实消息替换本地临时消息（避免重复显示）
      const dup = groupMessages.value.findLast(m => m.id < 0 && m.senderId === msg.senderId && m.content === msg.content)
      if (dup) {
        Object.assign(dup, msg)     // 替换为真实数据
      } else {
        groupMessages.value.push(msg)
      }
    })

    onUserJoined((userId, nickname) => {
      groupMessages.value.push({
        id: -Date.now(),
        content: `${nickname} 加入了群聊`,
        messageType: 'System',
        createdAt: new Date().toISOString()
      })
    })

    onUserLeft(userId => {
      groupMessages.value.push({
        id: -Date.now(),
        content: '有人离开了群聊',
        messageType: 'System',
        createdAt: new Date().toISOString()
      })
    })

    onPrivateChatRequest(req => {
      privateChatRequest.value = req
    })

    onPrivateChatAccepted(data => {
      privateChatId.value = data.chatId
    })

    onPrivateChatRejected(() => {
      privateChatId.value = null
    })

    onPrivateMessage(msg => {
      privateMessages.value.push(msg)
    })

    onIdentityRevealed(data => {
      // 更新消息列表中对应的发送者名字
    })

    onMemberKicked(userId => {
      groupMessages.value.push({
        id: -Date.now(),
        content: '有成员被踢出群聊',
        messageType: 'System',
        createdAt: new Date().toISOString()
      })
    })

    onMemberMuted((userId, isMuted) => {
      const action = isMuted ? '被禁言' : '被解除禁言'
      groupMessages.value.push({
        id: -Date.now(),
        content: `有成员${action}`,
        messageType: 'System',
        createdAt: new Date().toISOString()
      })
    })

    onGroupDissolved(() => {
      groupMessages.value.push({
        id: -Date.now(),
        content: '该群已被解散',
        messageType: 'System',
        createdAt: new Date().toISOString()
      })
    })

    onError(msg => {
      console.error('SignalR Error:', msg)
    })
  }

  // 加载群历史消息（分页）
  async function loadGroupHistory(groupId, page = 1) {
    loadingHistory.value = true
    try {
      const res = await getGroupMessages(groupId, page, 30)
      const list = res.data || []
      if (page === 1) {
        groupMessages.value = list
      } else {
        // 旧消息接到列表前面
        groupMessages.value = [...list, ...groupMessages.value]
      }
      hasMoreHistory.value = list.length >= 30
    } finally {
      loadingHistory.value = false
    }
  }

  // 加载私聊历史消息
  async function loadPrivateHistory(chatId, page = 1) {
    const res = await getPrivateMessages(chatId, page)
    privateMessages.value = res.data || []
  }

  // 进入群聊
  async function enterGroup(groupId) {
    groupMessages.value = []
    await joinGroup(groupId)
    await loadGroupHistory(groupId)
  }

  // 离开群聊
  async function exitGroup(groupId) {
    await leaveGroup(groupId)
    groupMessages.value = []
  }

  // 发送群消息——先本地显示（带 status），再通过 SignalR 发出去
  async function sendMessage(groupId, content, messageType = 0, fileUrl = null) {
    const userJson = localStorage.getItem('user')
    const myId = userJson ? JSON.parse(userJson).id : 0
    const typeName = ['Text', 'Image', 'File'][messageType] || 'Text'
    const tempId = -Date.now()
    const msg = {
      id: tempId,
      groupId,
      senderId: myId,
      senderNickname: '我',
      content,
      messageType: typeName,
      fileUrl,
      createdAt: new Date().toISOString(),
      status: 'sending'
    }
    groupMessages.value.push(msg)
    try {
      await sendGroupMessage(groupId, content, messageType, fileUrl)
      // 服务端回推时会替换；这里也标记一下，防止替换不及
      const m = groupMessages.value.find(x => x.id === tempId)
      if (m) m.status = 'sent'
    } catch {
      const m = groupMessages.value.find(x => x.id === tempId)
      if (m) m.status = 'failed'
    }
  }

  // 加载我加入的群（用于侧边栏）
  async function loadMyGroups() {
    try {
      const res = await getMyGroups()
      myGroups.value = res.data || []
    } catch {
      myGroups.value = []
    }
  }

  // 发起私聊
  async function startPrivateChat(targetUserId) {
    privateChatRequest.value = null
    await requestPrivateChat(targetUserId)
  }

  // 响应私聊
  async function acceptPrivateChat(chatId, accepted) {
    await respondPrivateChat(chatId, accepted)
  }

  // 发送私聊消息
  async function sendPrivate(chatId, content, messageType = 0, fileUrl = null) {
    await sendPrivateMessage(chatId, content, messageType, fileUrl)
  }

  // 暴露身份
  async function reveal(chatId) {
    await revealIdentity(chatId)
  }

  return {
    connected, groupMessages, privateMessages, privateChatId, privateChatRequest,
    myGroups, loadingHistory, hasMoreHistory,
    connect, disconnect,
    loadGroupHistory, loadPrivateHistory, loadMyGroups,
    enterGroup, exitGroup,
    sendMessage, startPrivateChat, acceptPrivateChat, sendPrivate, reveal
  }
})
