import csv, os, subprocess, pathlib, html, statistics as st

H = pathlib.Path(__file__).resolve().parent
EDGE = r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
log = os.path.expandvars(r'%USERPROFILE%\AppData\LocalLow\DefaultCompany\Monday\playtest_log.csv')
rows = [r for r in csv.DictReader(open(log)) if '2026-11-25 10:00' < r['utc_time'] < '2026-11-25 11:27']
d = {}
for r in rows:
    key = r['detail'] if r['event'] == 'task_completed' else r['event']
    d.setdefault(r['player_id'], {})[key] = float(r['seconds_since_start'])
S = [(k, v['alarm_off'], v['brush_teeth'] - v['alarm_off'], v['game_completed']) for k, v in d.items()]
assert len(S) == 10


def table(head, body, w=None):
    cg = ''.join(f'<col style="width:{x}%">' for x in w) if w else ''
    out = '<table><colgroup>' + cg + '</colgroup><thead><tr>' + ''.join(f'<th>{c}</th>' for c in head) + '</tr></thead><tbody>'
    for r in body:
        out += '<tr>' + ''.join(f'<td>{c}</td>' for c in r) + '</tr>'
    return out + '</tbody></table>'


CSS = '''@page{size:A4;margin:12mm 14mm}
body{font:8.6pt/1.25 "Segoe UI",Arial,sans-serif;color:#111;margin:0}
h1{font-size:15pt;margin:0 0 1px}
h2{font-size:10.5pt;margin:6px 0 2px;border-bottom:1px solid #999;break-after:avoid}
p{margin:2px 0 4px}
table{border-collapse:collapse;width:100%;margin:3px 0 2px;font-size:7.9pt;line-height:1.18}
th,td{border:1px solid #bbb;padding:1.5px 4px;text-align:left;vertical-align:top}
th{background:#e8edf3}
tr,.k{break-inside:avoid}
.c{font-size:7.6pt;color:#333;margin:0 0 4px}
.todo{background:#fff2a8;border:1px dashed #b58900;padding:3px 6px;margin:3px 0}
.two{display:flex;gap:10px}.two>div{flex:1}
ol{margin:2px 0}'''


def pdf(name, title, body):
    f = H / (name + '.html')
    f.write_text(f'<!doctype html><html lang="en"><meta charset="utf-8"><title>{title}</title>'
                 f'<style>{CSS}</style><body>{body}</body></html>', encoding='utf-8')
    out = H / (name + '.pdf')
    out.unlink(missing_ok=True)
    subprocess.run([EDGE, '--headless', '--disable-gpu', '--no-pdf-header-footer',
                    '--user-data-dir=' + os.path.join(os.environ['TEMP'], 'm3edge'),
                    f'--print-to-pdf={out}', f.as_uri()], check=True, capture_output=True, timeout=90)


survey = [
    ('Movement and camera control felt responsive', 4.33, .71, 8),
    ('Picking up and dropping objects felt intuitive', 3.78, 1.09, 5),
    ('Clear which objects I could interact with', 2.89, 1.27, 3),
    ('Wake-up sequence simulated drowsiness', 4.22, .97, 8),
    ('Understood the minigame without instructions', 3.33, .87, 3),
    ('Conveyed the Monday morning feeling', 4.00, 1.12, 6),
    ('I would want to keep playing', 4.22, .67, 8),
]


def bar():
    o = '<svg viewBox="0 0 520 146" width="100%" role="img">'
    for i, (n, m, _, _) in enumerate(survey):
        y = 4 + i * 18
        w = (m - 1) * 62
        color = '#c0562b' if m < 3 else '#4472a8'
        o += f'<text x="226" y="{y+10}" font-size="10" text-anchor="end">{n}</text>'
        o += f'<rect x="232" y="{y}" width="{w:.1f}" height="13" fill="{color}"/>'
        o += f'<text x="{236+w:.1f}" y="{y+10}" font-size="10">{m:.2f}</text>'
    o += '<line x1="356" x2="356" y1="0" y2="130" stroke="#222" stroke-dasharray="4 3"/>'
    o += '<text x="356" y="142" font-size="9.5" text-anchor="middle">target 3.0</text>'
    o += '<text x="232" y="142" font-size="9.5">1</text><text x="480" y="142" font-size="9.5" text-anchor="middle">5</text></svg>'
    return o


def stack():
    k = 3.4
    o = '<svg viewBox="0 0 520 168" width="100%" role="img">'
    o += '<rect x="60" y="0" width="10" height="10" fill="#c0562b"/><text x="74" y="9" font-size="10">alarm off</text>'
    o += '<rect x="140" y="0" width="10" height="10" fill="#4472a8"/><text x="154" y="9" font-size="10">alarm off to teeth 100% clean</text>'
    for i, (_, a, b, t) in enumerate(S):
        y = 16 + i * 15
        o += f'<text x="54" y="{y+9}" font-size="10" text-anchor="end">S{i+1}</text>'
        o += f'<rect x="60" y="{y}" width="{a*k:.1f}" height="11" fill="#c0562b"/>'
        o += f'<rect x="{60+a*k:.1f}" y="{y}" width="{b*k:.1f}" height="11" fill="#4472a8"/>'
        o += f'<text x="{64+t*k:.1f}" y="{y+9}" font-size="9.5">{t:.1f} s</text>'
    return o + '</svg>'


A, B, T = ([s[i] for s in S] for i in (1, 2, 3))


def stat(n, x):
    return [n, f'{st.mean(x):.1f}', f'{st.median(x):.1f}', f'{st.stdev(x):.1f}', f'{min(x):.1f} to {max(x):.1f}']


questions = table(['Design question', 'Evidence used'], [
    ['Does player control support real-time movement and camera use?', 'Movement and drowsiness ratings; input notes'],
    ['Do players discover objects and tasks unaided?', 'Alarm questions; object clarity rating; alarm time'],
    ['Is the brushing minigame understandable, and does it support skill and flow?', 'Minigame rating; exit-rule item; brushing time'],
    ['Does the game convey a Monday morning?', 'Monday feeling rating; comments; Play Matrix'],
], [56, 44])

team = table(['Conducted by', 'Players', 'Sessions'], [
    ['Ayaan', 'P1, P2, P3', '3'], ['Qasim', 'P4, P5, P6', '3'], ['Yusra', 'P7, P8, P9, P10', '4']])

ratings = table(['Item (1 to 5)', 'Mean', 'SD', 'Rated 4 or 5'],
                [[n, f'{m:.2f}', f'{s:.2f}', f'{k} of 9'] for n, m, s, k in survey], [64, 10, 10, 16])

items = table(['Item', 'Result'], [
    ['Turned off alarm without help', 'Yes 3 (33%); No 6 (67%)'],
    ['Reported time to find alarm', 'Under 30 s: 5; 30 to 60 s: 3; over 60 s: 1'],
    ['Completed the minigame', '9 of 9'],
    ['Clarity of 90% exit rule', 'Very clear 1; had an idea 4; not clear 4']], [46, 54])

observations = table(['Theme', 'Players', 'n', 'Detail'], [
    ['Acting before the alarm is off', '2, 4, 6', '3', 'Walked (P2, P4) or brushed teeth (P6) while it was ringing'],
    ['Finding or using the alarm', '3, 4, 7, 10', '4', '10 to 15 s to realise (P3); unsure what to do (P4); which button (P7); larger interaction area wanted (P10)'],
    ['Collision and physics', '2, 3, 5, 6', '4', 'No colliders: grill, stove (P2), closet (P5), toilet (P6); bottle entered sofa, pants flew away (P3)'],
    ['Doors', '3, 7, 8', '3', 'Slammed into player (P3, P8); would not open when standing in front (P7)'],
    ['Tried missing interactions', '2, 4', '2', 'Toilet and shower (P2); non-interactive objects (P4)'],
    ['Input confusion', '3, 7', '2', 'Esc unclear (P3); left click vs left arrow, arrow keys instead of WASD (P7)'],
    ['Brushing minigame', '5, 8, 9', '3', 'Cleans at cursor, not brush head (P5); progress bar more prominent (P8, P9)'],
    ['Art direction', '5, 6, 9', '3', 'Liked lighting (P6) and aesthetic (P9); washing machine inconsistent (P5)'],
    ['More minigames or interactions', '7, 8, 9, 10', '4', 'Laundry, window, plant, shower, books, bed, sandwich, alarm minigame'],
    ['Other direction', '4, 6, 7', '3', 'Task list, lack of incentive (P4); narrative over minigames, messier room (P6, P7); explore, not timed (P7)'],
], [20, 11, 4, 65])

changes = table(['Priority', 'Planned modification', 'Evidence'], [
    ['High', 'Block movement (already in revised build) and other interactions until the alarm is off; enlarge its interaction area; prompt after a short delay', '3 of 10 acted early; 4 of 10 had difficulty; 6 of 9 needed help'],
    ['High', 'Highlight interactive objects on aim; make plant, books and bottle interactive or clearly decorative', 'Mean 2.89; 2 of 10 tried missing interactions; 2 comments'],
    ['High', 'Add colliders (grill, stove, closet, toilet); stop small objects entering furniture', '4 of 10; 1 comment'],
    ['High', 'Open doors away from the player and at close range', '3 of 10'],
    ['Medium', 'Minigame: review the automatic 100% ending (already replaces Esc), enlarge progress bar, clean at brush head', '4 of 9 unclear on exit; mean 3.33; 3 of 10'],
    ['Medium', 'Controls panel at start', '2 of 10 input confusion'],
    ['Low', 'Task list; 1 or 2 simple interactions such as watering the plant; review narrative direction; replace washing machine', '4 of 10 want more; 2 of 10 prefer narrative; 1 of 10 each for task list and asset'],
], [9, 54, 37])

report = f'''<h1>Monday: Playtesting Report (Milestone 3)</h1>
<p>CS 319 Introduction to Game Design and Development, Habib University, Fall 2026. Team: Ayaan, Qasim, Yusra</p>
<h2>1. Approach and target areas</h2>
<p><i>Monday</i> is a first-person slice-of-life game set in one bedroom: the player turns off an alarm, gets up and brushes their teeth. Ten players took part in observed sessions, above the minimum of 6 for a team of 3. Observers took notes on behaviour, confusion and bugs, and 9 of the 10 players submitted a survey. Unity Analytics sessions were recorded as a third source.</p>
<div class="todo"><b>TO ADD:</b> one sentence on player backgrounds (how many outside the university, majors). Only P7's is recorded: does not play video games.</div>
{questions}<p class="c">The game has no NPCs, so NPC movement was not tested.</p>
{team}
<p class="c"><b>Two builds.</b> The observation notes and survey describe the M2 build (brushing ends with Esc at 90%; walking possible before the alarm is off). The Analytics sessions ran on a revised build (brushing ends automatically at 100%; walking locked until the player is up). Results are therefore reported separately, and S1 to S10 are not matched to P1 to P10.</p>
<h2>2. Survey results (n = 9, M2 build)</h2>
{ratings}
<div class="two k"><div>{bar()}<p class="c">Figure 1. Mean rating per item.</p></div><div>{items}
<p><b>Comments.</b> Four players commented and 3 were informative: missing collision on the bottle and toilet and a wish to leave the minigame early; plant and books looked interactive but were not; unsure what to do with the bottle.</p></div></div>
<h2>3. Observation findings (n = 10, M2 build)</h2>
{observations}
<p class="c">P1 closed the alarm instantly and found the minigame unaided. P8 and P9 closed the alarm first, but the observer flagged both as possibly biased.</p>
<h2>4. Play Matrix</h2>
<div class="todo"><b>TO ADD (required):</b> Play Matrix chart with each player's placement (chance to skill, mental to physical), the number of players per quadrant, and how it compares with the team's intended placement.</div>
<h2>5. Unity Analytics (10 logged sessions, revised build)</h2>
<p>Two custom events were implemented: <b>task_completed</b> (task_name, String: alarm_off or brush_teeth) and <b>game_completed</b> (total_time, Float). Each playthrough received a random anonymous user ID. Times below come from the local log written alongside these events; timing starts when Play is pressed. A team test run on 24 November is excluded.</p>
<div class="two k"><div>{stack()}<p class="c">Figure 2. Time per task by session.</p></div><div>{table(['Seconds', 'Mean', 'Median', 'SD', 'Range'], [stat('Alarm off', A), stat('Alarm off to clean', B), stat('Total', T)])}
<p><b>Funnel:</b> started 10, alarm off 10, teeth brushed 10, completed 10 (100%).</p>
<p><b>Alarm time varied most:</b> {max(A)/min(A):.1f} times between fastest and slowest, against {max(B)/min(B):.1f} times for the next interval. {sum(a < 30 for a in A)} of 10 took under 30 s and {sum(a >= 30 for a in A)} took 30 to 60 s. Finding the first task is less consistent than the task that follows, which includes getting up and walking to the sink.</p>
<p><b>Slower starts went with slower brushing</b> (r = {st.correlation(A, B):.2f}, n = 10), so total time may reflect general familiarity. The sample is too small to establish a cause.</p></div></div>
<div class="todo"><b>TO ADD:</b> dashboard screenshots (Event Browser, Data Explorer by task_name). Confirm that S1 to S10 are separate external players and who ran them.</div>
<h2>6. Analysis and planned modifications</h2>
<p><b>Meets target.</b> For an early build the target was a mean of at least 3.0 per item. Six of 7 items met it. Movement, drowsiness and wanting to keep playing were each rated 4 or 5 by 8 of 9 players, and 2 of 10 players praised the visual style. <b>Needs refinement.</b> Discoverability is the main weakness: object clarity was the only item below target (2.89), with 4 of 9 rating it 2 or below, and 6 of 9 needed help with the alarm. Two items that met the target were still rated 3 or lower by several players: picking up objects (4 of 9) and understanding the minigame (6 of 9). Completion does not measure engagement, and 4 of 9 found the exit rule unclear. Survey and notes differ on the alarm (6 of 9 against 4 of 10), possibly because observers noted only visible hesitation.</p>
{changes}
<p class="c"><b>Limitations:</b> small sample; 1 player did not submit the survey; 2 possibly biased alarm observations; two builds; every Analytics session counts as a new user, so retention was not analysed.</p>'''
pdf('M3_Playtest_Report', 'Monday: Playtesting Report', report)

notes = [
    ('1', '', ['Instantly knew to close the alarm', 'Explored pick-up mechanics', 'Found the teeth game without guidance']),
    ('2', '', ['Bug: Player can move without closing the alarm', 'Bug: No colliders on grill or stove', 'Tried to poop and shower']),
    ('3', '', ['Took 10–15 seconds to realize the alarm needs to be closed', 'Door slamming in their face', 'Esc was unclear → teeth game', 'Bottle went inside the sofa', 'Pants flew away']),
    ('4', '', ['Walking bug before alarm closed', 'Unclear about what to do', "Tried to interact with stuff that couldn't be interacted with", '★ Suggestion: task list', 'Lacks incentive and motivation']),
    ('5', '', ['Closet collider not present', 'Teeth game cleans at the mouse cursor, not the head of the brush', 'Washing machine not consistent with art style', '(left blank)']),
    ('6', '', ['Brushed teeth before closing alarm', 'No collider on toilet', 'Liked the lighting', 'Make the room worse (messier)', 'Add narrative instead of minigames']),
    ('7', " (doesn't play video games)", ["Didn't know which button to press to close the alarm", 'Confused between left click and left arrow key', 'Used arrow keys instead of WASD', "Door doesn't open when standing right in front of it", '★ Add laundry game → make room messier', '★ More interactions, like watering the plant', '★ Window game', 'Prefers narrative and interaction over minigames', 'Wants to explore the room rather than be timed']),
    ('8', '', ['Closed alarm first (bias: [name removed])', 'Door slamming issue', 'Make progress bar more prominent in teeth game', '★ Minigame to close the alarm', 'Shower minigame', 'Wants more minigames']),
    ('9', '', ['Closed alarm (bias)', 'Suggestion: books minigame', 'Suggestion: watering the plant minigame', 'Suggestion: making the bed minigame', 'Make progress bar more visible', 'Loved the aesthetic']),
    ('10', '', ['Make the interaction area with the alarm bigger', 'Sandwich-making thing', 'Game input (could also read "LAME INPUT")']),
]


def who(n):
    return 'Ayaan' if n < 4 else 'Qasim' if n < 7 else 'Yusra'


body = "<h1>Monday: Observation Notes</h1><p>Typed from the observers' playtest notes. One name removed for privacy.</p>"
for n, x, l in notes:
    body += f'<div class="k"><h2>Player {n}{html.escape(x)}, conducted by {who(int(n))}</h2><ol>'
    body += ''.join(f'<li>{html.escape(i)}</li>' for i in l) + '</ol></div>'
pdf('Observation_Notes', 'Monday: Observation Notes', body)

body = '<h1>Monday: Analytics Events and Session Log</h1><h2>Custom events</h2>'
body += table(['Event', 'Parameter', 'Sent when'], [
    ['task_completed', 'task_name (String)', 'Alarm turned off (alarm_off); teeth reach 100% (brush_teeth)'],
    ['game_completed', 'total_time (Float, seconds)', 'Both tasks are done']])
body += '<p>Each playthrough uses a random anonymous user ID. session_start and session_end are written to the local log only.</p>'
body += '<h2>Local session log, 25 November 2026 (UTC)</h2>'
body += table(['UTC time', 'ID', 'Event', 'Detail', 'Seconds'],
              [[r['utc_time'], r['player_id'], r['event'], r['detail'], r['seconds_since_start']] for r in rows])
pdf('Analytics_Events_and_Log', 'Monday: Analytics Events and Session Log', body)
