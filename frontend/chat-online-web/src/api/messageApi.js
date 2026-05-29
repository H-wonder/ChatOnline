import httpClient from './httpClient'

// 获取群历史消息
export function getGroupMessages(groupId, page = 1, pageSize = 50) {
  return httpClient.get(`/groups/${groupId}/messages`, { params: { page, pageSize } })
}

// 获取私聊历史消息
export function getPrivateMessages(chatId, page = 1, pageSize = 50) {
  return httpClient.get(`/private-chats/${chatId}/messages`, { params: { page, pageSize } })
}

// 删除自己的消息
export function deleteMessage(id) {
  return httpClient.delete(`/messages/${id}`)
}

// 上传文件
export function uploadFile(file) {
  const formData = new FormData()
  formData.append('file', file)
  return httpClient.post('/files/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
}

// 获取用户资料
export function getUserProfile() {
  return httpClient.get('/users/me')
}

// 修改用户资料
export function updateProfile(data) {
  return httpClient.put('/users/me', data)
}
