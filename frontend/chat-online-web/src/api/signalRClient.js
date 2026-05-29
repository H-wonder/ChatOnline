import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

let connection = null

// 建立 SignalR 连接
export function startConnection() {
  const token = localStorage.getItem('token')

  connection = new HubConnectionBuilder()
    .withUrl('/hubs/chat', {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect()        // 断线自动重连
    .configureLogging(LogLevel.Information)
    .build()

  return connection.start()
}

// 停止连接
export async function stopConnection() {
  if (connection) {
    await connection.stop()
    connection = null
  }
}

// 获取连接实例（给 Vue 组件注册事件用）
export function getConnection() {
  return connection
}

// ===== 消息监听 =====

// 收到群消息
export function onGroupMessage(callback) {
  connection?.on('ReceiveGroupMessage', callback)
}

// 有人加入群
export function onUserJoined(callback) {
  connection?.on('UserJoinedGroup', callback)
}

// 有人退出群
export function onUserLeft(callback) {
  connection?.on('UserLeftGroup', callback)
}

// 收到私聊请求
export function onPrivateChatRequest(callback) {
  connection?.on('ReceivePrivateChatRequest', callback)
}

// 私聊被接受
export function onPrivateChatAccepted(callback) {
  connection?.on('PrivateChatAccepted', callback)
}

// 私聊被拒绝
export function onPrivateChatRejected(callback) {
  connection?.on('PrivateChatRejected', callback)
}

// 收到私聊消息
export function onPrivateMessage(callback) {
  connection?.on('ReceivePrivateMessage', callback)
}

// 身份暴露通知
export function onIdentityRevealed(callback) {
  connection?.on('IdentityRevealed', callback)
}

// 成员被踢
export function onMemberKicked(callback) {
  connection?.on('MemberKicked', callback)
}

// 成员被禁言
export function onMemberMuted(callback) {
  connection?.on('MemberMuted', callback)
}

// 群被解散
export function onGroupDissolved(callback) {
  connection?.on('GroupDissolved', callback)
}

// 错误事件
export function onError(callback) {
  connection?.on('Error', callback)
}

// ===== 服务端调用 =====

// 加入群聊 SignalR Group
export async function joinGroup(groupId) {
  await connection?.invoke('JoinGroup', groupId)
}

// 离开群聊 SignalR Group
export async function leaveGroup(groupId) {
  await connection?.invoke('LeaveGroup', groupId)
}

// 发送群消息
export async function sendGroupMessage(groupId, content, messageType = 0, fileUrl = null) {
  await connection?.invoke('SendGroupMessage', groupId, content, messageType, fileUrl)
}

// 发起私聊请求
export async function requestPrivateChat(targetUserId) {
  await connection?.invoke('RequestPrivateChat', targetUserId)
}

// 响应私聊请求
export async function respondPrivateChat(chatId, accepted) {
  await connection?.invoke('RespondPrivateChat', chatId, accepted)
}

// 发送私聊消息
export async function sendPrivateMessage(chatId, content, messageType = 0, fileUrl = null) {
  await connection?.invoke('SendPrivateMessage', chatId, content, messageType, fileUrl)
}

// 暴露身份
export async function revealIdentity(chatId) {
  await connection?.invoke('RevealIdentity', chatId)
}
