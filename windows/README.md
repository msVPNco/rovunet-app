<div align="center">

# Rovunet · Windows

**0.1.4 Beta · Windows x64 · prerelease**

[Download / دریافت](https://github.com/msVPNco/rovunet-app/releases/tag/windows-v0.1.4-beta)

[فارسی](#فارسی) · [English](#english) · [简体中文](#简体中文) · [Русский](#русский)

</div>

## فارسی

رابط HTML/CSS با سه پوستهٔ روشن، تیره و AMOLED، قاب جمع‌وجور، منوی کناری در نمای گسترده و پرچم‌های وسط‌چین. نوشته‌های بلند فقط هنگام فعال‌بودن پنجره حرکت می‌کنند. دکمه‌ها وضعیت فشرده‌شدن، انجام کار و توقف را نشان می‌دهند.

تست سریع برای مسیرهای عادی است: TCP یا دست‌دهی واقعی HY2، TUIC و WireGuard/AmneziaWG؛ تست دقیق از HTTPS استفاده می‌کند. تونل‌های DNS در جست‌وجوی ریزالور بررسی می‌شوند و به تست سریع یا جایگزینی خودکار اضافه نمی‌شوند. تعداد مسیرهای داخل و خارج آزمون روشن است؛ انتخاب دستی مستقل از نتیجهٔ تست است.

اشتراک دلخواه با دکمه‌های کپی و چسباندن، منابع مهسا و انونیموس، ویرایش پروفایل‌ها، وضعیت حساب، پراکسی محلی HTTP/SOCKS5 و پراکسی اختیاری ویندوز در دسترس‌اند. نشانی منابع آماده پنهان می‌ماند. گواهی Slipstream از سرور اشتراک و پس از بررسی هویت و هش دریافت می‌شود؛ سرور باید مسیر گواهی معتبر را ارائه کند. خطا، داده و گواهی قبلی را پاک نمی‌کند.

هدف این نسخه Windows10 22H2/build19045 و Windows11 x64 است. نصب‌کننده امضای ناشر ویندوز ندارد. بعضی مسیرهای آزمایشی سرور در تست زنده تأیید نشده‌اند؛ عرضهٔ یک آداپتور به معنی کارکرد همهٔ سرورها نیست. اجزای آزمایشگاهی TUN/kill switch/service در این نسخهٔ مصرفی فعال نمی‌شوند.

[راهنما](docs/USER-GUIDE-FA.txt) · [وضعیت و محدودیت‌ها](docs/STATUS.md) · [آزمون‌ها](docs/TESTING.md) · [تغییرات این مرحله](docs/LIVE35.md) · [ساخت](docs/BUILDING.md)

## English

A compact HTML/CSS interface with light, dark and AMOLED themes, a wide-window sidebar, centered language flags and overflow text that moves only while the app is active. Buttons show pressed, busy and Stop states.

Quick tests use TCP or authenticated HY2, TUIC and WireGuard/AmneziaWG protocol probes. Accurate tests use HTTPS. DNS tunnels are checked separately in Resolver Scanner and excluded from quick tests and automatic replacement; eligible/excluded counts are explicit. Manual connection remains independent of test results.

Includes subscription Copy/Paste, managed Mahsa/Anonymous sources, profile editors, account status, local HTTP/SOCKS5 and optional Windows system proxy. Managed source addresses stay hidden. Automatic Slipstream certificates require a valid same-source companion, bound identity and DER hash; failures preserve previous data and pins.

Targets Windows10 22H2/build19045 and Windows11 x64. The installer is not Authenticode-signed. Some supplied server paths remain unverified in live tests; adapter availability does not certify every provider. Laboratory TUN, kill-switch and service components are not activated by this consumer prerelease.

[Status](docs/STATUS.md) · [Testing](docs/TESTING.md) · [Current scope](docs/LIVE35.md) · [Build](docs/BUILDING.md) · [Historical README](docs/README-HISTORY-BEFORE-0.1.4.md)

## 简体中文

紧凑的 HTML/CSS 界面，支持浅色、深色和 AMOLED 主题、宽窗口侧栏及居中的语言旗帜。长文本仅在窗口处于活动状态时滚动，按钮显示按下、执行和停止状态。

快速测试使用 TCP 或 HY2、TUIC、WireGuard/AmneziaWG 的真实协议验证；精确测试使用 HTTPS。DNS 隧道在解析器扫描中单独测试，不参与快速测试或自动切换，并显示纳入和排除的数量。手动连接不依赖测试结果。

支持订阅复制与粘贴、Mahsa/Anonymous、配置编辑、账户状态、本地 HTTP/SOCKS5 和可选的 Windows 系统代理。自动获取 Slipstream 证书需要服务器提供同源证书文件，并通过身份与哈希验证；失败时保留原有数据和证书。

适用于 Windows10 22H2/build19045 和 Windows11 x64。安装程序没有 Authenticode 签名，部分服务器路径尚未通过实际网络测试。本消费预发布版本不启用实验性的 TUN、断网保护或系统服务组件。

## Русский

Компактный интерфейс HTML/CSS со светлой, тёмной и AMOLED темами, боковым меню в широком окне и флагами языков по центру. Длинный текст движется только в активном окне. Кнопки показывают нажатие, выполнение и остановку.

Быстрая проверка использует TCP или подтверждённый обмен HY2, TUIC и WireGuard/AmneziaWG; точная — HTTPS. DNS-туннели проверяются отдельно в сканере резолверов и исключены из быстрой проверки и автоматической замены. Количество включённых и исключённых маршрутов указано явно. Ручное подключение не зависит от проверки.

Доступны копирование и вставка подписки, Mahsa/Anonymous, редакторы профилей, состояние счёта, локальные HTTP/SOCKS5 и необязательный системный прокси Windows. Для автоматического сертификата Slipstream сервер должен предоставить файл с того же источника; проверяются привязка личности и хеш DER. Ошибка не удаляет прежние данные и сертификаты.

Для Windows10 22H2/build19045 и Windows11 x64. Установщик не подписан Authenticode; часть серверных маршрутов ещё не подтверждена реальными тестами. Лабораторные компоненты TUN, kill switch и системной службы в этой потребительской предварительной версии не активируются.

## Source and licences

The release includes the exact Windows source archive, provenance and checksums.
Pinned native engines and their corresponding source offers retain their own
licences. See [notices](NOTICE.md) and the source offer shipped with
the installer. Previous release tags and assets are preserved.
