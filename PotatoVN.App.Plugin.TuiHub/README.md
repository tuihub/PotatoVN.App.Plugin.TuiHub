# PotatoVN.App.Plugin.TuiHub

TuiHub 插件，用于连接 TuiHub 服务器，同步和下载应用。

## 已实现功能

### 1. 基础设施 ✅
- ✅ 项目结构和依赖配置
- ✅ 使用 TuiHub.Protos NuGet 包（版本 0.6.2）
- ✅ 数据模型（Settings, Tokens, PluginData, Cache）

### 2. 认证服务 ✅
- ✅ 账户密码登录（TuiHubAuthService）
- ✅ Token 自动刷新机制
- ✅ Token 持久化存储（通过 IPotatoVNApi）
- ✅ gRPC 客户端拦截器（ClientTokenInterceptor）
- ✅ gRPC 通道工厂（GrpcChannelFactory）

### 3. 数据同步 ✅
- ✅ 应用和分类缓存存储（TuiHubCacheStore）
- ✅ 后台同步任务（TuiHubSyncAppsTask）
  - 支持分页获取应用列表
  - 获取分类列表并建立关联
  - 进度报告和取消支持

### 4. 下载功能 ✅
- ✅ 下载任务（TuiHubDownloadTask）
  - 调用 DownloadStoreAppBinary API
  - 使用 Downloader 库进行多线程下载
  - Steam 风格目录结构（downloading/temp/common）
  - 进度报告和取消支持
  - PostDownload 占位方法（待实现解压/安装逻辑）

### 5. 设置界面 ✅
- ✅ 设置页面（TuiHubSettingsPage）
  - 配置 Librarian URL
  - 配置下载根目录
  - 登录/退出功能
  - 手动触发同步
  - 缓存信息显示和清除

### 6. 插件主类 ✅
- ✅ Plugin.cs 基础实现
  - 实现 IPlugin 接口
  - 实现 IPluginSetting 接口
  - 数据加载和保存
  - 服务初始化

## 待实现功能

### 1. 虚拟游戏库 🚧
需要实现：
- 按 AppCategory 创建虚拟库
- 未分类应用归入"TuiHub 未分类"库
- 库提供者服务（实现 ISourceProvider 接口）
- 虚拟游戏对象创建

### 2. 下载按钮集成 🚧
需要实现：
- 为未下载的应用添加下载菜单项
- 集成到主程序的游戏卡片菜单
- 下载状态显示

### 3. 完善 Plugin.cs 🚧
需要添加：
- 注册库提供者
- 注册菜单项
- 事件处理

## 技术细节

### 目录结构
```
PotatoVN.App.Plugin.TuiHub/
├── Models/
│   ├── TuiHubSettings.cs
│   ├── Tokens.cs
│   ├── PluginData.cs
│   └── Cache/
│       ├── CachedApp.cs
│       ├── CachedCategory.cs
│       └── AppCache.cs
├── Services/
│   ├── Auth/
│   │   └── TuiHubAuthService.cs
│   ├── Cache/
│   │   └── TuiHubCacheStore.cs
│   └── Grpc/
│       ├── GrpcChannelFactory.cs
│       └── ClientTokenInterceptor.cs
├── BgTasks/
│   ├── TuiHubSyncAppsTask.cs
│   └── TuiHubDownloadTask.cs
├── UI/
│   └── Settings/
│       ├── TuiHubSettingsPage.xaml
│       └── TuiHubSettingsPage.xaml.cs
└── Plugin.cs
```

### 依赖项
- TuiHub.Protos (0.6.2)
- Grpc.Net.Client (2.70.0)
- Downloader (3.3.1)
- Microsoft.WindowsAppSDK (1.8.250907003)

### 配置
- **Librarian URL**: 默认 `http://localhost:3100`
- **下载目录结构**: 
  - `downloading/`: 正在下载的文件
  - `temp/`: 临时文件
  - `common/`: 最终游戏文件

## 使用说明

1. 在插件设置中配置 Librarian URL 和下载目录
2. 使用账户密码登录 TuiHub
3. 点击"同步应用库"按钮同步应用和分类
4. （待实现）在游戏库中查看 TuiHub 应用
5. （待实现）点击下载按钮下载应用

## 注意事项

- Token 会自动刷新，无需手动处理
- 所有网络操作都通过后台任务执行
- 下载的文件需要手动解压/安装（PostDownload 功能待实现）
- 命名空间冲突：使用 `global::TuiHub.Protos` 避免与插件命名空间冲突

