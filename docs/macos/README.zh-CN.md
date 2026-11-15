[English](README.md) · [فارسی](README.fa.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md)

# Rovunet macOS 0.1.8 Beta

**Pre-release · internal build 0.1.7.20 · macOS 15+**

| Mac | 安装包与校验和 | 验证 |
| --- | --- | --- |
| Apple Silicon · arm64 | [PKG](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-AppleSilicon-arm64.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | 已安装；界面与日常 TUN 获用户确认 |
| Intel · x64 | [PKG · NOT-TESTED](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-Intel-x64-NOT-TESTED.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | **未在真实 Intel Mac 上测试** |

适用于 **macOS 15 及以上版本**的预览版，分别提供 Apple Silicon 和 Intel 安装包。公开版本为 **0.1.8 Beta**，内部构建号为 **0.1.7.20**。

### 打开未使用 Apple 签名的安装包

此预览版使用本地 ad hoc 签名，没有 Apple Developer ID 签名或公证，因此 macOS 可能显示“Apple could not verify…”或“Not Opened”。

1. 仅从 [Rovunet 官方发布页](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.8-beta)下载符合处理器的 PKG，并与该版本的 `SHA256SUMS.txt` 核对哈希。
2. 打开 PKG；出现 Apple 无法验证的提示时，点按 **Done（完成）**。
3. 打开 **Apple 菜单 → System Settings（系统设置）→ Privacy & Security（隐私与安全性）**，向下滚动至 **Security（安全性）**。
4. 找到针对**这个 Rovunet 文件**的阻止提示，点按 **Open Anyway（仍要打开）**。
5. 阅读后续警告。只有信任文件来源时，才**亲自点按 Open（打开），并自行完成 Touch ID 或 Mac 密码确认**。
6. 继续安装；升级时保留应用数据及 Keychain 项目。如果安装后的 Rovunet 应用也被阻止，可针对该应用重复这些步骤。

如果没有 Open Anyway，请再次尝试打开同一文件，然后返回隐私与安全性；受管理的 Mac 可能受到管理员策略限制。不要全局关闭 Gatekeeper、通过终端移除 quarantine 或启用“Anywhere”。此说明适用于开发者未验证的提示；若 macOS 明确提示文件会损害电脑或已损坏，请停止安装并检查文件及哈希。

[Apple 官方说明](https://support.apple.com/en-us/102445)

DNS 扫描结果卡片更紧凑，技术详情可通过箭头展开。DNS、隧道、HTTPS、进度和错误信息仍然可见；展开详情不会选择该解析器。快速检测与精确 HTTPS 测试分开；受支持的 UDP 协议使用经过身份验证的协议响应，单独的成功 ping 不代表数据能够通过。保留先前的界面、连接设置及面板订阅兼容性修复。

已在 Apple Silicon Mac 上安装并启动，**492 个安装文件**与安装包一致。**2139 项界面检查全部通过**；用户确认界面和日常 TUN 使用正常。先前构建 19 的测试中，**18/20 条快速线路**及之后使用 TCP 解析器的 **8/8 条 DNS 线路**（含 SSH）传输了完整 HTTPS 响应体。这不是构建 20 的全协议复测。用户报告加拿大 Panel 2.0.2 除 TUIC 外均可连接，其他客户端的 TUIC 也失败，仍需面板或服务器排查。另一个服务器上 WireGuard 的历史失败不能推广到此环境。

安装前请断开连接并使用 Command-Q 退出，选择与处理器匹配的 PKG。升级时保留配置和 Keychain。应用使用本地 **ad hoc 签名，没有 Apple Developer ID 签名及公证**；macOS 可能显示安全提示，请勿全局关闭 Gatekeeper。**Intel 尚未经过实体设备测试。** 强制退出恢复、DNS/路由恢复、睡眠唤醒、kill-switch 和长期能耗尚未完成全面验收。全局 TUN 下的独立 Smart 比较仍有限制。当前面板的 Slipstream 证书信任交付仍需复核，不绕过 TLS 或证书 pin 检查。通过 HY2 内嵌诊断得到的 WG/TUIC 成功不等同于直接连接成功。

## Source / منشأ / Исходники / 源码

[准确源码 ZIP](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-source.zip) · [Verification](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/VERIFICATION.json) · [Release notes](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.8-beta) · [Four-language record](releases/0.1.8-beta.md)

Exact local source snapshot: `71adb8900c68fb3b97f97903877902db718f586a`. This snapshot is not an upstream-published application commit. The attached source ZIP corresponds to these binaries; GitHub's automatic source archive is not asserted to match it. All eleven native engines and the TUN helper are reused unchanged from internal 0.1.7.19. No fresh full native rebuild or exhaustive protocol acceptance is claimed. Use the macOS release channel rather than repository-wide Latest. PKG installers only; no DMG is provided.

[Main project](../../README.zh-CN.md) · [Previous macOS preview](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.7-beta)
