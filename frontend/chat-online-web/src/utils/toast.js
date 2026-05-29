// 轻量 toast 替代 Element Plus ElMessage / ElMessageBox（省掉 356KB）

const container = document.createElement('div')
container.className = 'toast-container'
document.body.appendChild(container)

function show(text, type) {
  const el = document.createElement('div')
  el.className = `toast toast-${type}`
  el.textContent = text
  container.appendChild(el)
  requestAnimationFrame(() => el.classList.add('in'))
  setTimeout(() => {
    el.classList.remove('in')
    setTimeout(() => el.remove(), 300)
  }, 2500)
}

export const toast = {
  success: (m) => show(m, 'success'),
  error: (m) => show(m, 'error'),
  warning: (m) => show(m, 'warning'),
  info: (m) => show(m, 'info'),
}

export function confirm(title, message) {
  return new Promise((resolve) => {
    const overlay = document.createElement('div')
    overlay.className = 'confirm-overlay'
    overlay.innerHTML = `
      <div class="confirm-box">
        <h3>${title}</h3>
        <p>${message}</p>
        <div class="confirm-btns">
          <button class="confirm-cancel">取消</button>
          <button class="confirm-ok">确定</button>
        </div>
      </div>`
    document.body.appendChild(overlay)
    requestAnimationFrame(() => overlay.classList.add('in'))
    overlay.querySelector('.confirm-cancel').onclick = () => { close(); resolve(false) }
    overlay.querySelector('.confirm-ok').onclick = () => { close(); resolve(true) }
    overlay.onclick = (e) => { if (e.target === overlay) { close(); resolve(false) } }
    function close() { overlay.classList.remove('in'); setTimeout(() => overlay.remove(), 200) }
  })
}
