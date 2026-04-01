# Android 构建指南

## CI 自动构建 (推荐)

项目已配置 GitHub Actions 自动构建 Android APK。

### 触发构建
1. 推送到 `main` 分支会自动触发构建
2. 手动触发：GitHub → Actions → Godot Build → Run workflow

### 构建产物
- APK 文件：GitHub Actions → Artifacts → `android-apk`
- 自动签名：使用 debug keystore（仅用于测试）

### 生产环境发布
对于 Google Play 发布，需要：
1. 创建正式签名密钥
2. 在 GitHub Secrets 中配置：
   - `ANDROID_KEYSTORE` - Base64 编码的 keystore 文件
   - `ANDROID_KEYSTORE_PASSWORD` - 密钥库密码
   - `ANDROID_KEY_ALIAS` - 密钥别名
   - `ANDROID_KEY_PASSWORD` - 密钥密码

---

## 本地构建

### 前置要求

1. **Godot 4.6.1**
2. **Android SDK** ( cmdline-tools, platform-tools, build-tools )
3. **JDK 17**
4. **Godot Android 导出模板**

### 步骤 1: 安装 Android SDK

```bash
# 使用 SDK Manager 安装必需组件
sdkmanager "cmdline-tools;latest"
sdkmanager "platform-tools"
sdkmanager "platforms;android-34"
sdkmanager "build-tools;34.0.0"
```

### 步骤 2: 下载 Godot Android 模板

从 https://github.com/godotengine/godot-builds/releases/download/4.6.1-stable/Godot_v4.6.1-stable_android_build_template.zip 下载

解压到 Godot 模板目录：
- Windows: `%APPDATA%/Godot/templates/4.6.1.stable/`
- Linux: `~/.local/share/godot/templates/4.6.1.stable/`
- macOS: `~/Library/Application Support/Godot/templates/4.6.1.stable/`

### 步骤 3: 配置导出预设

在 Godot 编辑器中：
1. 项目 → 导出 → Android
2. 配置：
   - **Gradle Build**: 启用
   - **架构**: arm64-v8a (必需), armeabi-v7a (可选)
   - **包名**: com.rhythmx.app
   - **最小 SDK**: 24 (Android 7.0)
   - **目标 SDK**: 34 (Android 14)
   - **版本**: 1.0.0 (code: 1)

### 步骤 4: 签名配置

#### Debug 构建（测试用）
```bash
# 生成 debug keystore
keytool -genkey -v -keystore debug.keystore \
  -storepass android -alias androiddebugkey \
  -keyalg RSA -keysize 2048 -validity 10000 \
  -dname "CN=Android Debug,O=Android,C=US"
```

#### Release 构建（发布用）
```bash
# 生成正式签名密钥
keytool -genkey -v -keystore rhythmx-release.keystore \
  -alias rhythmx \
  -keyalg RSA -keysize 2048 -validity 10000
```

### 步骤 5: 导出 APK

在 Godot 编辑器中：
1. 项目 → 导出
2. 选择 Android
3. 选择导出路径
4. 点击 "导出项目"

---

## 故障排查

### 问题：找不到导出模板
```
ERROR: Template not found for version "4.6.1.stable"
```
**解决**: 确保模板文件位于正确目录，文件夹名为 `4.6.1.stable`

### 问题：Gradle 构建失败
```
FAILURE: Build failed with an exception
```
**解决**:
1. 检查 `ANDROID_HOME` 环境变量
2. 接受 SDK 许可证：`sdkmanager --licenses`
3. 清理 Gradle 缓存：删除 `~/.gradle/caches/`

### 问题：APK 未签名
```
FAILURE: Build failed: Keystore was not found
```
**解决**:
1. 在导出预设中配置正确的 keystore 路径
2. 或在项目设置中指定 `android_keystore.properties`

---

## 构建输出

| 类型 | 文件 | 用途 |
|------|------|------|
| Debug APK | RhythmX-debug.apk | 测试、开发 |
| Release APK | RhythmX-release.apk | 分发给用户 |
| AAB | RhythmX-release.aab | Google Play 发布 |

---

## 配置说明

### 权限配置
当前配置包含以下权限：
- `INTERNET` - 网络访问（API 调用）
- `ACCESS_NETWORK_STATE` - 网络状态查询
- `READ/WRITE_EXTERNAL_STORAGE` - 歌曲导入

### 架构支持
- ✅ arm64-v8a (现代 Android 设备)
- ❌ armeabi-v7a (旧设备，可启用)
- ❌ x86/x86_64 (模拟器，可启用)

### 版本配置
- **version/code**: 整数，每次发布递增
- **version/name**: 语义化版本号 (如 1.0.0)

---

## GitHub Actions 配置

当前 CI 工作流支持：
- ✅ Windows 桌面版
- ✅ Linux 桌面版
- ✅ Web (HTML5)
- ✅ Android (APK)

所有构建产物将上传为 GitHub Artifacts，有效期 90 天。
