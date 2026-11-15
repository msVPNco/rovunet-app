[English](README.md) · [فارسی](README.fa.md) · [Русский](README.ru.md) · [简体中文](README.zh-CN.md)

# Rovunet macOS 0.1.8 Beta

**Pre-release · internal build 0.1.7.20 · macOS 15+**

| Mac | Установщик и контрольная сумма | Проверки |
| --- | --- | --- |
| Apple Silicon · arm64 | [PKG](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-AppleSilicon-arm64.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | Установка, интерфейс и повседневный TUN подтверждены |
| Intel · x64 | [PKG · NOT-TESTED](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-Intel-x64-NOT-TESTED.pkg) · [SHA-256](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/SHA256SUMS.txt) | **НЕ ПРОВЕРЕН на физическом Intel Mac** |

Предварительная версия для **macOS 15+**; отдельные установщики Apple Silicon и Intel. Версия **0.1.8 Beta**, внутренняя сборка **0.1.7.20**.

### Открытие пакета без подписи Apple

В этой версии используется локальная подпись ad hoc; Apple Developer ID и нотариальное заверение отсутствуют. Поэтому macOS может показать «Apple could not verify…» или «Not Opened».

1. Скачайте PKG для своего процессора только из [официального выпуска Rovunet](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.8-beta) и сверьте хеш с `SHA256SUMS.txt` этого выпуска.
2. Откройте PKG. При предупреждении о проверке Apple нажмите **Done (Готово)**.
3. Откройте **меню Apple → System Settings (Системные настройки) → Privacy & Security (Конфиденциальность и безопасность)** и прокрутите до **Security (Безопасность)**.
4. У сообщения о блокировке **именно этого файла Rovunet** нажмите **Open Anyway (Всё равно открыть)**.
5. Прочитайте предупреждение. Только если доверяете источнику файла, **самостоятельно нажмите Open и подтвердите действие через Touch ID или пароль Mac**.
6. Продолжите установку. При обновлении сохраните данные приложения и Keychain. Если после установки заблокировано само приложение Rovunet, повторите эти действия для него.

Если Open Anyway отсутствует, ещё раз попробуйте открыть тот же файл и вернитесь в настройки; на управляемом Mac действие может запрещаться администратором. Не отключайте Gatekeeper глобально, не удаляйте quarantine через Terminal и не включайте «Anywhere». Инструкция относится к непроверенному разработчику; при явном сообщении об опасном или повреждённом файле остановитесь и проверьте файл/хеш.

[Официальная инструкция Apple](https://support.apple.com/en-us/102445)

Результаты DNS Scanner стали компактнее, технические подробности раскрываются стрелкой. Статусы DNS, туннеля и HTTPS, ход проверки и ошибки остаются видимыми; раскрытие не выбирает DNS. Быстрая проверка отделена от точного HTTPS-теста. Для поддерживаемых UDP-протоколов используются аутентифицированные ответы протокола; один успешный пинг не доказывает передачу данных. Сохранены предыдущие исправления интерфейса, настроек и совместимости подписок панели.

На Apple Silicon приложение установлено и запущено; **492 файла** совпали с пакетом. **2139 проверок интерфейса, без ошибок**. Владелец подтвердил внешний вид и повседневную работу TUN. В предыдущей сборке 19 полное тело HTTPS прошло через **18/20 быстрых путей** и затем **8/8 DNS-путей**, включая SSH, с TCP-резолверами. Это не полный повтор тестов сборки 20. По отзыву владельца, в Канаде с Panel 2.0.2 работает всё, кроме TUIC; его сбой в нескольких клиентах требует проверки панели/сервера. Старый результат WireGuard на другом сервере рассматривается отдельно.

Перед установкой отключитесь и закройте приложение через Command-Q. Выберите PKG своего процессора; сохраните профили и Keychain. Подпись приложения локальная **ad hoc; Apple Developer ID и notarization отсутствуют**. Возможны предупреждения macOS; не отключайте Gatekeeper глобально. **Intel на реальном оборудовании не проверен.** Восстановление после принудительного завершения, DNS/маршрутов, сон/пробуждение, kill-switch и длительное энергопотребление ещё не прошли полную приёмку. Независимое Smart-сравнение при глобальном TUN ограничено. Доставку доверенного сертификата Slipstream на текущей панели нужно перепроверить; TLS/pin не обходятся. Проверка WG/TUIC внутри HY2 не равна прямому соединению.

## Source / منشأ / Исходники / 源码

[ZIP точных исходников](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/Rovunet-macOS-0.1.8-beta-source.zip) · [Verification](https://github.com/msVPNco/rovunet-app/releases/download/macos-v0.1.8-beta/VERIFICATION.json) · [Release notes](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.8-beta) · [Four-language record](releases/0.1.8-beta.md)

Exact local source snapshot: `71adb8900c68fb3b97f97903877902db718f586a`. This snapshot is not an upstream-published application commit. The attached source ZIP corresponds to these binaries; GitHub's automatic source archive is not asserted to match it. All eleven native engines and the TUN helper are reused unchanged from internal 0.1.7.19. No fresh full native rebuild or exhaustive protocol acceptance is claimed. Use the macOS release channel rather than repository-wide Latest. PKG installers only; no DMG is provided.

[Main project](../../README.ru.md) · [Previous macOS preview](https://github.com/msVPNco/rovunet-app/releases/tag/macos-v0.1.7-beta)
