(() => {
  'use strict';
  const app = document.getElementById('app');
  const NS = 'http://www.w3.org/2000/svg';
  const paths = {
    power:'M12 2v9 M6 5a9 9 0 1 0 12 0', plus:'M12 2v20 M2 12h20',
    more:'M12 3v1 M12 11v1 M12 19v1', edit:'M2 18 16 4l4 4L6 22H2z M14 6l4 4',
    trash:'M4 6h16 M8 6V3h8v3 M6 6v15h12V6 M10 10v7 M14 10v7',
    layers:'m2 7 10-6 10 6-10 6z M2 12l10 6 10-6 M2 17l10 6 10-6',
    check:'M4 5h16v15H4z M8 2v6 M16 2v6 M8 13l3 3 6-6',
    stop:'M5 5h14v14H5z', up:'m4 16 8-8 8 8', down:'m4 8 8 8 8-8',
    search:'M10 2a8 8 0 1 0 0 16 8 8 0 1 0 0-16 M16 16l7 7',
    settings:'M4 6h16 M4 12h16 M4 18h16 M8 3v6 M16 9v6 M10 15v6'
  };
  let model = null, currentPage = '', lastBody = '', lastPopup = '', fieldTimers = new Map();
  let lastBase = '', popupNode = null, popupSignature = '';
  let hostFocused = false, motion = true;
  const pressedPointers=new Set();let deferredModel=null;
  const send = (command, id = '', value = '') => window.chrome?.webview?.postMessage({command, id:String(id), value:String(value)});
  // Accent colours come with the model, because every visual token is a CSS
  // variable. They are written on <body> — the element the theme blocks target —
  // so no theme (amoled included) can shadow them. High contrast keeps the
  // system Highlight colour instead.
  function accent(data){
    const body=document.body.style,palette=data&&data.accent;
    if(!palette){return;}
    for(const name of ['--accent','--group','--accent-soft','--accent-ring'])body.removeProperty(name);
    if(data.theme==='contrast'){return;}
    const dark=data.theme==='dark'||data.theme==='amoled';
    body.setProperty('--accent',dark?palette.dark:palette.light);
    body.setProperty('--group',dark?palette.groupDark:palette.groupLight);
    body.setProperty('--accent-soft',dark?palette.softDark:palette.softLight);
    body.setProperty('--accent-ring',dark?palette.ringDark:palette.ringLight);
  }
  // Live counters must not remove the button between pointer-down and click.
  document.addEventListener('pointerdown',event=>pressedPointers.add(event.pointerId),true);
  function releasePointer(event){pressedPointers.delete(event.pointerId);setTimeout(()=>{if(!pressedPointers.size&&deferredModel){const next=deferredModel;deferredModel=null;render(next);}},0);}
  document.addEventListener('pointerup',releasePointer,true);document.addEventListener('pointercancel',releasePointer,true);
  window.addEventListener('blur',()=>{pressedPointers.clear();if(deferredModel){const next=deferredModel;deferredModel=null;render(next);}});
  function el(tag, className='', text) {
    const node=document.createElement(tag); if(className)node.className=className;
    if(text!==undefined&&text!==null)node.textContent=String(text); return node;
  }
  function icon(name, path, filled=false, viewbox='0 0 24 24') {
    const svg=document.createElementNS(NS,'svg'); svg.setAttribute('viewBox',viewbox);svg.setAttribute('aria-hidden','true');
    const shape=document.createElementNS(NS,'path');shape.setAttribute('d',(path||paths[name]||paths.more).replace(/^F[01]\s*/,''));
    shape.setAttribute('fill',filled?'currentColor':'none');shape.setAttribute('fill-rule','evenodd');shape.setAttribute('stroke',filled?'none':'currentColor');shape.setAttribute('stroke-width','1.7');shape.setAttribute('stroke-linecap','round');shape.setAttribute('stroke-linejoin','round');svg.append(shape);return svg;
  }
  function brand() {
    const node=el('div','brand');node.append(icon('', 'M4 0H329C416 0 466 57 466 148C466 238 416 296 334 296L476 482H317L178 296H129V482H2Z M127 101H333V201H127Z M0 516H476V534H0Z',true,'0 0 476 534'),el('span','','ovunet'));return node;
  }
  function button(label, action, image, className='') {
    const node=el('button',className);node.type='button';node.setAttribute('aria-label',label);node.title=label;node.addEventListener('click',event=>{event.stopPropagation();flushFields();action(event);});
    if(image)node.append(icon(image));return node;
  }
  function roll(text, className='', dir='auto') {
    const node=el('div','roll '+className);node.dir=dir;node.append(el('span','',text));return node;
  }
  function measureRolls() {
    // Read every width first, then write: one layout pass instead of one per
    // label. A collection with many rows used to stall while it opened.
    const nodes=[...app.querySelectorAll('.roll')];
    const measured=nodes.map(node=>{const text=node.firstElementChild;if(!text)return null;return [Math.max(0,text.scrollWidth-node.clientWidth),node.dir==='rtl'];});
    nodes.forEach((node,index)=>{
      const value=measured[index];if(!value)return;
      const [excess,rtl]=value;
      node.dataset.overflow=String(excess>2);
      node.style.setProperty('--travel',(rtl?-excess:excess)+'px');
    });
    const header=app.querySelector('.topbar');
    if(header){const first=header.querySelector('.top-slot'),last=header.querySelector('.top-slot.end'),flags=header.querySelector('.flags');if(first&&last&&flags)header.classList.toggle('split',header.clientWidth<2*Math.max(first.scrollWidth,last.scrollWidth)+flags.scrollWidth+25);}
    const footer=app.querySelector('.footer'),content=app.querySelector('.content');if(footer&&content)content.style.setProperty('--footer-height',footer.offsetHeight+'px');
  }
  function activity() {document.body.classList.toggle('active',hostFocused&&motion&&!document.hidden&&document.hasFocus());document.body.classList.toggle('motion-off',!motion);}
  window.addEventListener('focus',activity);window.addEventListener('blur',activity);document.addEventListener('visibilitychange',activity);
  const sizeObserver=new ResizeObserver(()=>requestAnimationFrame(measureRolls));sizeObserver.observe(app);
  function fieldSend(id,value,immediate=false) {
    id=String(id);
    clearTimeout(fieldTimers.get(id)?.timer);if(immediate){fieldTimers.delete(id);send('native',id,value);return;}
    const entry={value,timer:0};entry.timer=setTimeout(()=>{fieldTimers.delete(id);send('native',id,value);},160);fieldTimers.set(id,entry);
  }
  // Commit the last keystroke before Save/navigation, including touch and
  // keyboard activation that may not blur the input first.
  function flushFields() {for(const [id,entry] of fieldTimers){clearTimeout(entry.timer);fieldTimers.delete(id);send('native',id,entry.value);}}
  function native(node) {
    if(!node)return document.createTextNode('');
    if(node.kind==='brand')return brand();
    let result;
    switch(node.kind){
      case 'toolbar':{
        result=el('header','n-toolbar');const children=node.children||[];if(children[0])result.append(native(children[0]));
        const title=el('div','page-title');children.filter(child=>child?.kind!=='button').forEach(child=>title.append(child.kind==='text'?roll(child.text):native(child)));result.append(title);
        const actions=el('div','page-actions');children.slice(1).filter(child=>child?.kind==='button').forEach(child=>actions.append(native(child)));result.append(actions);break;
      }
      case 'tabs':{
        result=el('section','n-stack');const bar=el('div','n-tabs');bar.setAttribute('role','tablist');(node.options||[]).forEach((label,index)=>{const tab=button(label,()=>send('native',node.id,index),'','n-button');tab.disabled=node.enabled===false;tab.textContent=label;tab.setAttribute('role','tab');tab.setAttribute('aria-selected',String(index===node.value));bar.append(tab);});result.append(bar);(node.children||[]).forEach(child=>result.append(native(child)));break;
      }
      case 'text':result=node.nowrap?roll(node.text,'n-text',node.direction||'auto'):el('span','n-text',node.text);if(node.size)result.style.fontSize=node.size+'px';if(node.weight)result.style.fontWeight=node.weight;break;
      case 'marquee':result=el('div','roll');{const span=el('span');(node.children||[]).forEach(child=>span.append(native(child)));result.append(span);}break;
      case 'icon':result=icon('',node.path,node.filled,node.viewbox);result.classList.add('n-icon');break;
      case 'image':result=el('img','n-image');result.src=node.image;result.alt='';if(node.width)result.style.width=node.width+'px';break;
      case 'input':case 'password':{
        result=el(node.multiline?'textarea':'input','n-input');if(result.tagName==='INPUT')result.type=node.kind==='password'?'password':'text';
        result.value=node.value||'';result.disabled=node.enabled===false;result.readOnly=!!node.readonly;result.autocomplete='off';result.spellcheck=false;
        result.dataset.field=String(node.id);result.addEventListener('input',()=>fieldSend(node.id,result.value));result.addEventListener('change',()=>fieldSend(node.id,result.value,true));break;
      }
      case 'select':result=el('select','n-select');(node.options||[]).forEach((label,index)=>{const option=el('option','',label);option.value=String(index);result.append(option);});result.selectedIndex=node.value;result.disabled=node.enabled===false;result.dataset.field=String(node.id);result.addEventListener('change',()=>send('native',node.id,result.selectedIndex));break;
      case 'scan-list':{
        result=el('div','n-stack scan-list');(node.rows||[]).forEach(entry=>{const row=el('label','scan-result n-surface n-row');const check=el('input');check.type='checkbox';check.checked=entry.selected;check.disabled=!entry.enabled;check.addEventListener('change',()=>send('scan-select',node.id,check.checked+'|'+entry.address));const copy=el('div','n-stack');copy.append(roll(entry.address,'profile-name','ltr'));const ping=el('small','',entry.transport+' · '+entry.ping+' · '+entry.checks);ping.style.color=entry.pingColor;const description=roll(entry.description,'profile-result');description.style.color=entry.color;copy.append(ping,description);row.append(check,copy);result.append(row);});
        if(node.total>150){const paging=el('div','n-row');const previous=button('‹',()=>send('scan-page',node.id,Math.max(0,node.offset-150)));previous.textContent='‹';previous.disabled=node.offset===0;const next=button('›',()=>send('scan-page',node.id,node.offset+150));next.textContent='›';next.disabled=node.offset+150>=node.total;paging.append(previous,el('span','',String(node.offset+1)+'–'+Math.min(node.total,node.offset+150)+' / '+node.total),next);result.append(paging);}break;
      }
      case 'check':case 'radio':{
        result=el('label','n-'+node.kind);const input=el('input');input.type=node.kind==='radio'?'radio':'checkbox';input.dataset.field=String(node.id);input.checked=!!node.checked;input.disabled=node.enabled===false;input.addEventListener('change',()=>send('native',node.id,input.checked));result.append(input);(node.children||[]).forEach(child=>result.append(native(child)));break;
      }
      case 'button':result=button(node.label||'',()=>send('native',node.id),'','n-button'+(node.danger?' danger':'')+(node.swatch?' swatch':''));result.disabled=node.enabled===false;result.dataset.native=String(node.id);if(node.swatch)result.style.setProperty('--swatch',node.swatch);if(node.swatch&&node.selected)result.setAttribute('aria-selected','true');if(node.icon)result.append(icon('',node.icon));(node.children||[]).forEach(child=>result.append(native(child)));break;
      case 'field':result=el('label','n-field');result.append(el('span','',node.label));(node.children||[]).forEach(child=>result.append(native(child)));break;
      case 'progress':result=el('progress','n-progress');result.max=node.max;result.value=node.value;result.setAttribute('aria-label',model?.strings?.MORE||'Progress');break;
      case 'slider':result=el('input');result.type='range';result.dataset.field=String(node.id);result.disabled=node.enabled===false;result.min=node.min;result.max=node.max;result.value=node.value;result.addEventListener('change',()=>send('native',node.id,result.value));break;
      case 'expander':result=el('details','n-expander');result.open=node.checked;{const summary=el('summary','',node.label);summary.setAttribute('aria-disabled',String(node.enabled===false));summary.addEventListener('click',event=>{if(node.enabled===false)event.preventDefault();});result.append(summary);}(node.children||[]).forEach(child=>result.append(native(child)));result.addEventListener('toggle',()=>{if(node.enabled!==false&&result.open!==node.checked)send('native',node.id,result.open);});break;
      default:result=el('div','n-'+node.kind);if(node.kind==='grid'){result.style.gridTemplateColumns=node.columns;if(node.uniform)result.dataset.uniform='true';}(node.children||[]).forEach(child=>{if(child){const item=native(child);if(node.kind==='grid'&&item.style){item.style.gridColumn=(child.column+1)+' / span '+(child.colspan||1);item.style.gridRow=(child.row+1)+' / span '+(child.rowspan||1);}result.append(item);}});
    }
    if(result.style){if(node.color&&node.color!=='inherit')result.style.color=node.color;if(node.direction)result.dir=node.direction;if(Number.isFinite(node.width)&&!['input','password','select','image'].includes(node.kind))result.style.width=node.width+'px';}
    return result;
  }
  function header(data) {
    const bar=el('header','topbar'),leading=el('div','top-slot'),trailing=el('div','top-slot end');leading.append(native(data.header.leading));trailing.append(native(data.header.trailing));
    const flags=el('div','flags');flags.setAttribute('aria-label','Language');
    data.flags.forEach(flag=>{const item=button(flag.label,()=>send('language','',flag.code),'','flag');item.setAttribute('aria-pressed',String(data.language===flag.code));const image=el('img');image.src=flag.image;image.alt='';item.append(image);flags.append(item);});bar.append(leading,flags,trailing);return bar;
  }
  function sidebar(data) {
    const nav=el('nav','sidebar');nav.append(brand());const bottom=el('div','nav-bottom');
    data.navigation.forEach(item=>{const row=button(item.label,()=>send('native',item.id),'','nav-item');row.append(icon('',item.path),el('span','',item.label));if(item.selected)row.setAttribute('aria-current','page');(item.key==='HELP'||item.key==='ABOUT'?bottom:nav).append(row);});nav.append(bottom);return nav;
  }
  function hero(data) {
    // Keep the scroll extent constant while the visible card contracts. Resizing
    // the scrolling child itself makes browser scroll anchoring feed back into
    // scrollTop, repeatedly expanding/collapsing it near the 60px boundary.
    const slot=el('div','hero-slot'),section=el('section','hero');section.dataset.test='hero';const frame=el('div','power-frame');const control=button(data.hero.title,()=>send('power'),'power','power '+(data.hero.state==='connected'?'connected':'')+(['connecting','disconnecting'].includes(data.hero.state)?' working':''));control.disabled=!data.hero.enabled;control.setAttribute('aria-busy',String(['connecting','disconnecting'].includes(data.hero.state)));frame.append(control);
    const copy=el('div','hero-copy'),title=roll(data.hero.title,'hero-title');title.style.color=data.hero.color;copy.append(title,roll(data.hero.name,'hero-name'));
    const meta=el('div','hero-meta'),description=roll(data.hero.detail,'hero-detail');description.style.color=data.hero.detailColor;meta.append(description,el('span','hero-ping',data.hero.ping));copy.append(meta);section.append(frame,copy);slot.append(section);return slot;
  }
  function profile(item,data) {
    const row=el('article','profile'+(item.selected?' selected':'')+(item.active?' active':''));row.dataset.profile=item.id;row.tabIndex=0;row.setAttribute('role','button');row.setAttribute('aria-pressed',String(item.selected));row.addEventListener('click',()=>send('profile',item.id));row.addEventListener('keydown',event=>{if(event.target===row&&['Enter',' '].includes(event.key)){event.preventDefault();send('profile',item.id);}});
    // Four-step speed stripe on the card edge, matching the Android client's tiers.
    if(item.speed){const stripe=el('span','speed-stripe');stripe.style.background=item.speed;stripe.setAttribute('aria-hidden','true');row.append(stripe);}
    const copy=el('div','profile-copy');copy.append(roll(item.name,'profile-name'),roll(item.host,'profile-host','ltr'));const info=roll(item.protocol+(item.result?' · '+item.result:''),'profile-result');info.style.color=item.resultColor;copy.append(info);row.append(copy);
    const ping=el('span','ping',item.ping||'—');ping.dir='ltr';ping.style.color=item.pingColor;row.append(ping);
    const actions=el('div','actions');const edit=button(data.strings.EDIT,()=>send('edit',item.id),'edit');edit.disabled=!item.edit;const more=button(data.strings.MORE,()=>send('profile-menu',item.id),'more');const remove=button(data.strings.DELETE,()=>send('delete',item.id),'trash');remove.disabled=!item.delete;actions.append(edit,more,remove);row.append(actions);return row;
  }
  function collection(group,data) {
    const section=el('section','collection');section.dataset.group=group.id;const head=el('div','collection-head'+(group.active?' active':''));
    const top=el('div','collection-top'),label=el('div','collection-label');label.append(icon('layers'));const identity=el('div','collection-identity');identity.append(roll(group.name,'collection-name'),roll(group.meta,'muted'));label.append(identity);label.addEventListener('click',()=>send('toggle',group.id));top.append(label);
    const actions=el('div','actions');const toggle=button(group.open?data.strings.HIDE_PATHS:data.strings.SHOW_PATHS,()=>send('toggle',group.id),group.open?'up':'down');toggle.setAttribute('aria-expanded',String(group.open));const test=button(group.running?data.strings.STOP_TEST:data.strings.QUICK_PING,()=>send(group.running?'stop':'ping',group.id),group.running?'stop':'check','test-button');test.dataset.test='quick-ping';test.dataset.state=group.running?'stop':'ping';test.disabled=group.stopping||(!group.running&&!group.testEnabled);test.setAttribute('aria-busy',String(group.running));const power=button(group.powerLabel||data.strings.CONNECT,()=>send('group-power',group.id),'power','group-power'+(group.active?' connected':''));power.disabled=group.powerEnabled===false;const more=button(data.strings.MORE,()=>send('group-menu',group.id),'more');actions.append(toggle,test,power,more);top.append(actions);head.append(top);
    if(group.activity){const activity=roll(group.activity.text,'collection-activity');activity.setAttribute('role','status');activity.setAttribute('aria-live','polite');activity.dataset.busy=String(group.activity.running);activity.style.color=group.activity.color;head.append(activity);}else if(group.status)head.append(roll(group.status,'collection-status'));
    // Usage and remaining time belong on the card itself, not only after it is
    // opened; Android shows them on the collapsed collection too.
    if(group.quota){const quota=el('div','quota');quota.append(roll(group.quota.used),roll([group.quota.remaining,group.quota.time,group.quota.note,group.quota.percent].filter(Boolean).join(' · ')));head.append(quota);}
    if(group.open&&group.summary&&!group.activity)head.append(roll(group.summary,'summary'));
    if(group.running){const progress=el('progress');progress.max=group.total;progress.value=group.progress;head.append(progress);}section.append(head);
    if(group.open){const profiles=el('div','profiles');group.rows.forEach(item=>profiles.append(profile(item,data)));section.append(profiles);}return section;
  }
  function footer(data) {
    const result=el('footer','footer'),state=roll(data.footer.title,'footer-status');state.style.color=data.footer.color;result.append(state,roll(data.footer.detail,'muted'));
    const traffic=el('div','footer-traffic');traffic.append(el('span','',data.footer.up+'  '+data.footer.down),el('span','',data.footer.total));result.append(traffic);
    if(data.footer.notice){const note=roll(data.footer.notice,'footer-note');note.style.color=data.footer.noticeColor;result.append(note);}if(data.footer.error){const error=roll(data.footer.error,'footer-note');error.style.color='var(--bad)';result.append(error);}return result;
  }
  function popup(items,data) {
    const shade=el('div','menu-shade');shade.addEventListener('click',()=>send('close-menu'));const menu=el('div','menu');menu.setAttribute('role','menu');menu.addEventListener('click',event=>event.stopPropagation());
    function append(entries,parent){entries.forEach(item=>{if(item.kind==='separator'){parent.append(el('hr'));return;}if(item.kind!=='item')return;if(item.children.length){const details=el('details');details.append(el('summary','',item.label));const child=el('div');append(item.children,child);details.append(child);parent.append(details);}else{const action=button(item.label,()=>send('native',item.id));action.textContent=(item.selected?'✓  ':'')+item.label;action.disabled=!item.enabled;action.setAttribute('role','menuitem');parent.append(action);}});}
    append(items,menu);shade.append(menu);return shade;
  }
  // The menu is its own layer. Opening or closing it used to rebuild the whole
  // shell, cards included, which is what made the drawer stutter on Windows.
  function syncPopup(data) {
    const next = data.popup ? JSON.stringify(data.popup) : '';
    if (next === popupSignature) return;
    popupSignature = next;
    if (popupNode) { popupNode.remove(); popupNode = null; }
    if (data.popup) { popupNode = popup(data.popup, data); document.body.append(popupNode); }
  }
  function render(data) {
    // Menus change the popup only. When nothing else moved, redraw just that
    // layer instead of replacing every card and re-measuring the page.
    const base = JSON.stringify(data, (key, value) => key === 'popup' ? undefined : value);
    if (base === lastBase) { model = data; syncPopup(data); return; }
    lastBase = base;
    const animations=new Map();
    const rollKey=node=>(node.closest('[data-profile]')?.dataset.profile||node.closest('[data-group]')?.dataset.group||node.closest('.hero')&&'hero'||node.closest('.footer')&&'footer'||'page')+':'+node.textContent;
    app.querySelectorAll('.roll').forEach(node=>{const animation=node.firstElementChild?.getAnimations()[0];if(animation)animations.set(rollKey(node),animation.currentTime);});
    model=data;hostFocused=!!data.focused;motion=!!data.motion;document.documentElement.lang=data.language;document.documentElement.dir=data.rtl?'rtl':'ltr';document.body.dataset.theme=data.theme;accent(data);activity();
    const focus=document.activeElement,field=focus?.dataset?.field,nativeId=focus?.dataset?.native,start=focus?.selectionStart,end=focus?.selectionEnd,pendingValue=field&&fieldTimers.has(field)?focus.value:null;
    const oldScroll=app.querySelector('.home-scroll'),homeOffset=oldScroll?.scrollTop||0,pageOffset=app.querySelector('.page-scroll')?.scrollTop||0;
    const shell=el('div','shell');shell.append(header(data));const work=el('div','workspace'+(data.compact?' compact':''));work.append(sidebar(data));const content=el('main','content');
    if(data.depth){const page=el('div','page-scroll');const body=el('div','native-page');body.append(native(data.page));page.append(body);content.append(page);}else{
      if(data.searchOpen){const query=el('input','query');query.value=data.query;query.placeholder=data.strings.SEARCH;query.dataset.field='query';query.addEventListener('input',()=>send('query','',query.value));query.addEventListener('keydown',event=>{if(event.key==='Enter')send('search');});content.append(query);}
      const home=el('div','home-scroll');home.append(hero(data));data.groups.forEach(group=>home.append(collection(group,data)));if(!data.groups.length){const empty=el('div','empty');empty.append(el('h2','',data.emptyTitle),el('small','',data.emptyHint));home.append(empty);}home.addEventListener('scroll',()=>collapse(home));content.append(home);const add=button(data.strings.ADD,()=>send('add'),'plus','add');add.disabled=data.busy;content.append(add);
    }
    content.append(footer(data));work.append(content);shell.append(work);app.replaceChildren(shell);syncPopup(data);
    const nextHome=app.querySelector('.home-scroll');if(nextHome){nextHome.scrollTop=homeOffset;collapse(nextHome);}const nextPage=app.querySelector('.page-scroll');if(nextPage&&currentPage===data.pageKey)nextPage.scrollTop=pageOffset;currentPage=data.pageKey;
    let replacement=field?app.querySelector('[data-field="'+CSS.escape(field)+'"]'):nativeId?app.querySelector('[data-native="'+CSS.escape(nativeId)+'"]'):null;
    if(replacement&&hostFocused){if(pendingValue!==null)replacement.value=pendingValue;replacement.focus({preventScroll:true});if(Number.isInteger(start)&&replacement.setSelectionRange)try{replacement.setSelectionRange(start,end);}catch{}}
    // Restore clocks before the next paint, including the frame that deactivates the window.
    measureRolls();app.querySelectorAll('.roll').forEach(node=>{const time=animations.get(rollKey(node)),animation=node.firstElementChild?.getAnimations()[0];if(animation&&Number.isFinite(time))animation.currentTime=time;});
  }
  function collapse(scroll){const card=scroll.querySelector('.hero');if(card){const fraction=Math.min(1,Math.max(0,scroll.scrollTop/60));card.style.setProperty('--collapse',fraction);card.classList.toggle('collapsed',fraction>=1);}}
  document.addEventListener('keydown',event=>{if(event.key==='Escape'&&model?.popup)send('close-menu');});
  window.chrome?.webview?.addEventListener('message',event=>{if(event.data?.version===1){if(pressedPointers.size)deferredModel=event.data;else{deferredModel=null;render(event.data);}}});
  send('ready');
})();
