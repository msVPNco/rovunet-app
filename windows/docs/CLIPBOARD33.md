# Local 0.1.4-preview.7

The custom subscription address field now has explicit Copy and Paste buttons. These use the host clipboard after a user action; WebView-wide clipboard permissions and default browser context menus remain unchanged. The field remains editable and the existing bridge flushes pending keystrokes before Copy. Empty or busy clipboard feedback is translated; a failed paste preserves the existing address. Clipboard controls are disabled while the subscription fetch runs.

Managed Mahsa/Anonymous fields and their clipboard actions stay hidden. Pasting a recognized managed address switches to the managed-source view and still requires consent before fetching. Copy/Paste alone never fetches, imports, saves or changes account data. Existing Mahsa fallback/decode/validation and partial add-only rules are unchanged.

`--ui-clipboard33` drives real pointer hits in the owned WebView across all four languages and three themes, including last-keystroke copy, empty/error behavior, hidden/stale managed actions and fetch-busy guards. A native Windows text-clipboard round trip is attempted only if every existing format can be captured and restored; other clipboard content is left untouched. No original clipboard contents enter reports. Exact-package receipts describe whether the native round trip ran and restoration succeeded.

The packaging metadata now correctly lists all copied native engines as reused. The handshake helper's earlier HANDSHAKE30 rebuild is identified as baseline provenance, not a new rebuild in this UI package. Engine bytes, TLS, networking and all earlier published/local delivery bytes are preserved. Owner installation, live Slipstream, TUN/SCM/reboot and Panel acceptance remain separate.
