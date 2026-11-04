"""Compile the approved Markdown into static content. Original prose is retained verbatim.
Delegated defaults are centralized in NovemberConfig; explicit story branches override them."""
from pathlib import Path
import json,re
root=Path(__file__).resolve().parents[1]
doc=(root/'docs/PLANNER_ORDER_2026-11-04.md').read_text(encoding='utf-8-sig')
target=root/'Assets/Resources/activity-content.json'
old=json.loads(target.read_text(encoding='utf-8-sig'))
balancepath=root/'Assets/Resources/balance.json'
balance=json.loads(balancepath.read_text(encoding='utf-8-sig'))
blocks=re.findall(r'^#### ([A-Z]+(?:-N)?-?\d+) — ([^\n]+)\n(.*?)(?=^#### |\Z)',doc,re.M|re.S)
assert len(blocks)==174 and len({x[0] for x in blocks})==174
areas={'반파된 거주지':1,'침수된 지하철역':2,'박물관':3,'폐마트':4,'군사시설':5,'종교시설':6,'테마파크':7,'문화유산':8,'균열':9}
relics={'MUS-04':'antikythera','MUS-06':'dagger','MUS-08':'nebra','ARM-10':'bicorne','FUN-08':'yata-mirror','HER-08':'green-dragon-blade'}
rare={'HOM-09':'lyra','SUB-N05':'nerissa','ARM-07':'kael','ARM-09':'beyr'}
chain={
'HOM-03':['HOM-02'],'HOM-09':['HOM-06'],'HOM-11':['guard_discovered'],'HOM-12':['guard_radio_found'],'HOM-13':['guard_storehouse_known'],'HOM-14':['defense_story_complete','guard_discovered'],
'MUS-02':['MUS-01'],'MUS-06':['MUS-05'],'MUS-08':['MUS-07'],'MUS-15':['restorer_note'],'MUS-16':['hidden_collection_route'],
'ARM-07':['ARM-06'],'ARM-09':['ARM-08'],'ARM-14':['augmented_orders_found'],'ARM-15':['training_protocol_disabled'],'ARM-16':['shooting_range_truth'],
'SUB-N14':['flood_map_found'],'SUB-N15':['drain_route_open'],
'MRT-09':['night_stock_clue'],'MRT-10':['pharmacy_code_clue'],
'SAN-14':['hymn_piece_found'],'SAN-15':['confessional_archive_found'],'SAN-16':['star_chart_known'],
'FUN-02':['last_parade_clue'],'FUN-03':['parade_warehouse_known'],'FUN-04':['parade_route_found'],'FUN-05':['stunt_actor_offer'],'FUN-07':['storybook_key'],
'HER-02':['castle_inspection_known'],'HER-03':['beacon_route_known'],'HER-04':['tomb_record_found'],'HER-05':['gatekeeper_names_found'],'HER-06':['gatekeeper_names_found'],'HER-07':['golden_seal_cache']}
anychain={'MUS-04':['MUS-02','MUS-03'],'ARM-13':['ARM-06','ARM-08'],'FUN-08':['last_parade_clue','storybook_key'],'HER-08':['castle_inspection_known','golden_seal_cache']}
flags={'HOM-10':'guard_discovered','HOM-11':'guard_radio_found','HOM-12':'guard_storehouse_known','HOM-13':'defense_story_complete','HOM-14':'guard_recruited',
'SUB-N13':'flood_map_found','SUB-N14':'drain_route_open','SUB-N15':'theme_heritage_lore_found','SUB-N16':'two_destinations_foreshadow',
'MUS-14':'restorer_note','MUS-15':'hidden_collection_route','MUS-16':'restorer_story_complete',
'MRT-08':'night_stock_clue','MRT-09':'pharmacy_code_clue','MRT-10':'food_or_medicine_choice_record',
'ARM-13':'augmented_orders_found','ARM-14':'training_protocol_disabled','ARM-15':'shooting_range_truth','ARM-16':'humanity_story_complete',
'SAN-13':'hymn_piece_found','SAN-14':'confessional_archive_found','SAN-15':'star_chart_known','SAN-16':'pilgrim_story_complete',
'FUN-01':'last_parade_clue','FUN-02':'parade_warehouse_known','FUN-03':'parade_route_found','FUN-04':'stunt_actor_offer','FUN-05':'recruited_stunt_actor','FUN-06':'storybook_key','FUN-07':'storybook_pipe_story_complete',
'HER-01':'castle_inspection_known','HER-02':'beacon_route_known','HER-03':'tomb_record_found','HER-04':'gatekeeper_names_found','HER-05':'heritage_guard_recruit','HER-06':'golden_seal_cache','HER-07':'gate_seal_archive_complete'}
events=[]
def field(body,key):
    m=re.search(r'^- \*\*'+re.escape(key)+r':\*\* (.+)$',body,re.M)
    return m.group(1).strip() if m else ''
for eid,title,body in blocks:
    # Do not include subsequent section text in raw notes.
    body=re.split(r'^#{1,3} ',body,flags=re.M)[0]
    place=field(body,'장소') or field(body,'장소 / 유형')
    area=next((v for k,v in areas.items() if place.startswith(k)),None)
    assert area is not None,(eid,place)
    narrative=field(body,'현장 묘사')
    notes=field(body,'후속·주의') or field(body,'완료 플래그·후속') or field(body,'완료 조건') or field(body,'처리 조건')
    kind=place+field(body,'분류 / 반복 여부')
    repeat=bool(re.search(r'반복 식량|반복 회복|반복 복합|조건 반복|/ 반복$',kind)) or eid.startswith(('EX-','SU-','ZB-','SP-','SV-','PS-','DV-','RF-'))
    if not re.search(r'연쇄|연속|1회|성유물|희귀 NPC',kind) and eid not in chain and eid not in flags: repeat=True
    e=dict(id=eid,title=title,body=narrative,location=3 if area==9 else 0,area=area,scene='rift' if area==9 else 'chapel' if area==6 else 'ruins' if area==1 else 'area:'+str(area),repeat=repeat,prerequisites=chain.get(eid,[]),anyPrerequisites=anychain.get(eid,[]),rawNotes=notes,options=[],tags=[])
    if eid in relics:e['relicId']=relics[eid]
    if eid in rare:e['rareNpcId']=rare[eid]
    for i,letter in enumerate('ABC'):
        raw=field(body,'선택 '+letter)
        assert raw,(eid,letter)
        if raw.startswith('제시하지 않음'):continue
        parts=re.split(r'\s*(?:→|:)\s*',raw,maxsplit=1)
        text=parts[0]
        effect=parts[1] if len(parts)>1 else raw
        o=dict(text=text,outcome=raw,completes=i<2,flags=[],partialFlags=[])
        h=re.search(r'(\d+)시간',effect)
        o['hours']=int(h.group(1)) if h else 0
        if not h and re.search(r'시간\s*(?:소비|소모|비용)|시간이 들',effect):o['defaultHours']=True
        combat_text=effect+' '+text
        combat_positive=re.search(r'좀비.*(?:전투|처치|정리|조우|마주)|전투\s*(?:후|뒤|결과|승리|위험|가능(?!\s*(?:한|여부|자))|$)',combat_text)
        combat_avoid=re.search(r'전투\s*(?:위험\s*)?회피|조우\s*회피|적\s*회피|위험\s*회피|전투\s*가능성\s*(?:감소|완화)|좀비\s*조우\s*조건\s*(?:감소|완화)',combat_text)
        if combat_positive and not combat_avoid:
            o['normal']=3 if re.search(r'여러|무리|좀비 떼',effect+text) else 1
        if o.get('normal') and '특수 좀비' in combat_text:o['special']=1;o['normal']=0
        if (o.get('normal') or o.get('special')) and re.search(r'전투\s*(?:위험|가능(?!\s*(?:한|여부|자)))|전투.*가능성|조우\s*(?:위험|가능(?!\s*(?:한|여부|자)))',effect):o['optionalEncounter']=True
        if re.search(r'(?:HP|체력)\s*(?:손실|피해|위험)|잔해 피해',effect) and '회피' not in effect:o['injury']=True
        if re.search(r'정신력.*(?:손실|소모|위험|소비)',effect) and not re.search(r'회피|없|부담 없이|부담없는',effect):o['mentalRisk']=True
        if re.search(r'식량.*(?:발견|수색|획득).*판정|식량 발견',effect):o.update(discoverFood=True,chance='food')
        if re.search(r'(?:키트|주사기|회복품|회복아이템).*(?:획득|발견|판정)',effect):o['medicalLoot']=True
        if o.get('medicalLoot') and not o.get('chance') and re.search(r'발견.*판정|발견판정',effect):o['chance']='medical'
        if re.search(r'식량\s*(?:소비|차감)|식량을\s*(?:나누|교환)|식량.*교환',text+effect):o['food']=1
        if '신위 이벤트 판정' in effect or re.search(r'신위\s*\+3~10',effect):o.update(divinity=True,chance='divine');e['divinityEvent']=True
        isrecruit=bool(re.search(r'(?:모집|영입)\s*(?:기회|가능|제안|판정|응답)|교섭 후 모집',effect)) and not re.search(r'보류|연기|없음|하지 않|않음|않고',effect)
        if eid=='HOM-14' and i<2:isrecruit=True
        if eid=='SV-009' and i==0:isrecruit=True
        if isrecruit and eid!='HOM-10':
            if eid in rare:o['rareNpcId']=rare[eid]
            else:o['recruit']=True;o['uniqueNpcId']='GUARD_DEFENSE_TEAM' if eid=='HOM-14' else 'NPC_'+eid
            if '기회' in effect or '판정' in effect:o['chance']='recruit'
        if eid in rare and i==0:o['rareNpcId']=rare[eid];o['chance']='recruit'
        if eid in rare and 'rareNpcId' not in o:o['completes']=False
        if eid in relics and i<2:o['relicId']=relics[eid]
        if i<2 and eid in flags:o['flags']=[flags[eid]]
        if i==2:o['partialFlags']=[eid+':partial']
        if eid=='MUS-14' and i==2:o['partialFlags']=['restorer_partial_note']
        if eid=='HOM-10' and i==2:o['completes']=False;o['flags']=[]
        if eid.startswith('TRN-'):o=dict(text=text,outcome=raw)
        if eid=='ARM-14' and i==1:o.update(normal=1,special=1)
        if eid=='SAN-04' and i==1:o.update(normal=1,encounterChance=.5,noEncounterSanity=8,mentalRisk=False)
        e['options'].append(o)
    if eid=='RF-005':
        e['options'][0].update(text='집결지의 일반·특수 좀비를 돌파한다',normal=2,special=1)
        e['rawNotes']+=' / 사용자 위임 보완: 특수 최대1, 선택 후 네임드30% 대체.'
    if eid=='FUN-08':e['options'][1]['outcome']='일반 좀비 전투 승리 시 야타의 거울 획득. 퇴각 시 보상 없음.'
    events.append(e)
supplements=[]
for e in old['activityEvents']:
    if e['id'] in ['R-01','R-02','R-03','R-04']:
        e.update(area=9,repeat=False,supplemental=True)
        supplements.append(e)
# Keep the four approved legacy rift heirloom routes alongside the 174 new IDs.
assert len(supplements)==4
old['activityEvents']=events+supplements
old['relicDefinitions']=[d for d in old['relicDefinitions'] if d['id'] not in ['yata-mirror','green-dragon-blade']]
old['relicDefinitions'] += [
dict(id='yata-mirror',name='야타의 거울',description='일본 신화의 거울. 자체 능력 없이 계승자의 생존과 전투 기록을 전승한다.',location=0),
dict(id='green-dragon-blade',name='청룡언월도',description='삼국지연의에 전하는 관우의 무기. 자체 능력 없이 계승자의 업적을 전승한다.',location=0)]
target.write_text(json.dumps(old,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
balancepath=root/'Assets/Resources/balance.json'
balance=json.loads(balancepath.read_text(encoding='utf-8-sig'))
balance.update(normalHp=30,normalAttack=5,returnPersonalityOffsets=[-.05,.10,0,-.10,-.10,.15])
# Explicit existing field heirloom costs are preserved at their new locations.
mapping={'F-01':'MUS-04','F-02':'MUS-08','F-03':'MUS-06','F-04':'ARM-10'}
balance['eventCosts']=[dict(c,id=mapping.get(c['id'],c['id'])) for c in balance['eventCosts'] if c['id'] in list(mapping)+list(mapping.values()) or c['id'] in ['R-01','R-02','R-03','R-04']]
balancepath.write_text(json.dumps(balance,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
audit={'approvedIds':[e['id'] for e in events],'supplementalIds':[e['id'] for e in supplements],'deletedIds':['SP-008','SV-007','SV-010','SU-001','SU-002','SU-006','SU-007','SU-010'],'count':174,'runtimeTotal':178,'proseSource':'PLANNER_ORDER_2026-11-04.md','delegatedDefaults':'NovemberConfig'}
(root/'docs/event-audit-2026-11-04.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('178 runtime events: 174 approved + 4 preserved rift heirloom events; 10 relic definitions')
