# ChatOnline 在线聊天系统

## 环境要求

- .NET SDK 8.0+
- Node.js 18+
- MySQL 8.0+
- Vue 3 + Vite

## 项目结构

```
代码/
├── chatonline.sln              # Visual Studio 解决方案文件
├── backend/ChatOnline.Api/     # ASP.NET Core 8 后端（25个.cs源文件）
│   ├── Program.cs              # 应用入口
│   ├── Controllers/            # HTTP API 控制器（5个，21个接口）
│   ├── Hubs/                   # SignalR 实时通信（ChatHub + ConnectionMapping）
│   ├── Services/               # 业务逻辑层（5个Service）
│   ├── Models/Entities/        # 数据库实体（6张表）
│   ├── Models/Dtos/            # 数据传输对象
│   ├── Models/Requests/        # 请求参数类
│   ├── Data/AppDbContext.cs    # EF Core 数据库上下文
│   ├── Migrations/             # 数据库迁移文件
│   └── appsettings.json        # 配置文件
│
└── frontend/chat-online-web/   # Vue 3 前端（18个源文件）
    ├── src/views/              # 6个页面组件
    ├── src/api/                # 5个网络层模块
    ├── src/stores/             # 2个Pinia Store
    ├── src/router/             # 路由配置
    ├── src/components/         # 公共组件
    ├── src/utils/              # 工具函数
    └── vite.config.js          # 构建配置
```

## 启动方式

### 1. 配置数据库

编辑 `backend/ChatOnline.Api/appsettings.json`，修改数据库连接字符串：

```json
"ConnectionStrings": {
  "Default": "Server=localhost;Database=chat_online;User=root;Password=你的密码;Charset=utf8mb4;"
}
```

确保 MySQL 已启动，然后执行数据库迁移：

```bash
cd backend/ChatOnline.Api
dotnet ef database update
```

### 2. 启动后端

```bash
cd backend/ChatOnline.Api
dotnet run
# 监听 http://localhost:5000
```

### 3. 启动前端（开发模式）

```bash
cd frontend/chat-online-web
npm install
npx vite --host
# 监听 http://localhost:5173
```

浏览器打开 `http://localhost:5173`

### 4. 生产部署模式

```bash
# 编译前端到后端 wwwroot
cd frontend/chat-online-web
npm install
npm run build

# 启动后端（同时提供前端静态文件）
cd backend/ChatOnline.Api
dotnet run --urls "http://0.0.0.0:5000"
```

浏览器打开 `http://localhost:5000`

## 技术栈

| 层次 | 技术 |
|------|------|
| 后端框架 | ASP.NET Core 8.0 |
| 实时通信 | SignalR |
| 数据库 | MySQL 8.0 + EF Core 8.0 |
| 认证 | JWT + BCrypt |
| 前端框架 | Vue 3 + Vite |
| 状态管理 | Pinia |
| 路由 | Vue Router |
| HTTP客户端 | Axios |

## 作者

- 姓名：霍建杰
- 学号：2024302111405
- 课程：软件构造基础
