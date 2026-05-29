import httpClient from './httpClient'

// 群聊大厅列表
export function getGroups(search) {
  return httpClient.get('/groups', { params: { search } })
}

// 我加入的群（侧边栏用）
export function getMyGroups() {
  return httpClient.get('/groups/mine')
}

// 创建群聊
export function createGroup(data) {
  return httpClient.post('/groups', data)
}

// 群聊详情
export function getGroupDetail(id) {
  return httpClient.get(`/groups/${id}`)
}

// 加入群聊
export function joinGroup(id, data) {
  return httpClient.post(`/groups/${id}/join`, data)
}

// 退出群聊
export function leaveGroup(id) {
  return httpClient.post(`/groups/${id}/leave`)
}

// 群成员列表
export function getGroupMembers(id) {
  return httpClient.get(`/groups/${id}/members`)
}

// 禁言/解禁
export function toggleMute(groupId, userId, mute) {
  return httpClient.put(`/groups/${groupId}/members/${userId}/mute`, mute)
}

// 设置管理员
export function setAdmin(groupId, userId, isAdmin) {
  return httpClient.put(`/groups/${groupId}/members/${userId}/role`, isAdmin)
}

// 踢出成员
export function kickMember(groupId, userId) {
  return httpClient.delete(`/groups/${groupId}/members/${userId}`)
}

// 解散群聊
export function dissolveGroup(id) {
  return httpClient.delete(`/groups/${id}`)
}

// 修改群信息
export function updateGroup(id, data) {
  return httpClient.put(`/groups/${id}`, data)
}
