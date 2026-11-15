[English](README.md) · [فارسی](README.fa.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md)

# Rovunet macOS 0.1.8 Beta

**Pre-release · internal build 0.1.7.20 · macOS 15+**

| Mac | 安装包与校验和 | 验证 |
| --- | --- | --- |
| Apple Silicon · arm64 | [PKG](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-AppleSilicon-arm64.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | 已安装；界面与日常 TUN 获用户确认 |
| Intel · x64 | [PKG · NOT-TESTED](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-Intel-x64-NOT-TESTED.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | **未在真实 Intel Mac 上测试** |

适用于 **macOS 15 及以上版本**的预览版，分别提供 Apple Silicon 和 Intel 安装包。公开版本为 **0.1.8 Beta**，内部构建号为 **0.1.7.20**。

DNS 扫描结果卡片更紧凑，技术详情可通过箭头展开。DNS、隧道、HTTPS、进度和错误信息仍然可见；展开详情不会选择该解析器。快速检测与精确 HTTPS 测试分开；受支持的 UDP 协议使用经过身份验证的协议响应，单独的成功 ping 不代表数据能够通过。保留先前的界面、连接设置及面板订阅兼容性修复。

已在 Apple Silicon Mac 上安装并启动，**492 个安装文件**与安装包一致。**2139 项界面检查全部通过**；用户确认界面和日常 TUN 使用正常。先前构建 19 的测试中，**18/20 条快速线路**及之后使用 TCP 解析器的 **8/8 条 DNS 线路**（含 SSH）传输了完整 HTTPS 响应体。这不是构建 20 的全协议复测。用户报告加拿大 Panel 2.0.2 除 TUIC 外均可连接，其他客户端的 TUIC 也失败，仍需面板或服务器排查。另一个服务器上 WireGuard 的历史失败不能推广到此环境。

安装前请断开连接并使用 Command-Q 退出，选择与处理器匹配的 PKG。升级时保留配置和 Keychain。应用使用本地 **ad hoc 签名，没有 Apple Developer ID 签名及公证**；macOS 可能显示安全提示，请勿全局关闭 Gatekeeper。**Intel 尚未经过实体设备测试。** 强制退出恢复、DNS/路由恢复、睡眠唤醒、kill-switch 和长期能耗尚未完成全面验收。全局 TUN 下的独立 Smart 比较仍有限制。当前面板的 Slipstream 证书信任交付仍需复核，不绕过 TLS 或证书 pin 检查。通过 HY2 内嵌诊断得到的 WG/TUIC 成功不等同于直接连接成功。

## Source / منشأ / Исходники / 源码

[准确源码 ZIP](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-source.zip) · [Verification](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/VERIFICATION.json) · [Release notes](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.8-beta) · [Four-language record](releases/0.1.8-beta.md)

Exact local source snapshot: `71adb8900c68fb3b97f97903877902db718f586a`. This snapshot is not an upstream-published application commit. The attached source ZIP corresponds to these binaries; GitHub's automatic source archive is not asserted to match it. All eleven native engines and the TUN helper are reused unchanged from internal 0.1.7.19. No fresh full native rebuild or exhaustive protocol acceptance is claimed. Use the macOS release channel rather than repository-wide Latest. PKG installers only; no DMG is provided.

[Main project](../../README.zh-CN.md) · [Previous macOS preview](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.7-beta)
