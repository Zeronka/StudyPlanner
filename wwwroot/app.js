'use strict';
let tasks = [], editing = null, busy = false;
const $ = id => document.getElementById(id);
async function request(url, options = {}) {
  const response = await fetch(url, { ...options, headers: { 'Content-Type': 'application/json' } });
  const data = response.status === 204 ? null : await response.json();
  if (!response.ok) throw new Error(data?.errors ? Object.values(data.errors).flat().join(' ') : data?.message || `Ошибка ${response.status}`);
  return data;
}
function message(text, error = false) { $('message').textContent = text; $('message').classList.toggle('error', error); }
function node(tag, text, className) { const e = document.createElement(tag); if (text) e.textContent = text; if (className) e.className = className; return e; }
function render() {
  const area = $('tasks'); area.replaceChildren();
  const filter = $('filter').value;
  const visible = tasks.filter(t => filter === 'all' || t.isCompleted === (filter === 'completed'));
  if (!visible.length) area.append(node('p', 'В этом списке пока нет задач.', 'empty'));
  for (const task of visible) {
    const card = node('article', '', 'task' + (task.isCompleted ? ' done' : ''));
    const info = node('div'); info.append(node('h3', task.title));
    const date = task.dueDate ? task.dueDate.split('-').reverse().join('.') : 'Без срока';
    info.append(node('p', `${task.subject} · ${date}`));
    const actions = node('div', '', 'actions');
    const status = node('button', task.isCompleted ? 'Вернуть' : 'Готово', 'secondary');
    status.setAttribute('aria-label', `${task.isCompleted ? 'Вернуть в работу' : 'Завершить'}: ${task.title}`);
    status.addEventListener('click', () => mutate(() => request(`/api/tasks/${task.id}`, { method: 'PUT', body: JSON.stringify({ ...task, isCompleted: !task.isCompleted }) })));
    const edit = node('button', 'Изменить', 'secondary'); edit.addEventListener('click', () => {
      editing = task; $('title').value = task.title; $('subject').value = task.subject; $('due').value = task.dueDate || '';
      $('form-title').textContent = 'Редактирование задачи'; $('save').textContent = 'Сохранить'; $('cancel').hidden = false; $('title').focus();
    });
    const remove = node('button', 'Удалить', 'secondary danger'); remove.addEventListener('click', () => {
      if (confirm(`Удалить задачу «${task.title}»?`)) mutate(async () => { await request(`/api/tasks/${task.id}`, { method: 'DELETE' }); if (editing?.id === task.id) reset(); });
    });
    [status, edit, remove].forEach(b => { b.disabled = busy; actions.append(b); });
    card.append(info, actions); area.append(card);
  }
  $('total').textContent = tasks.length;
  $('completed').textContent = tasks.filter(t => t.isCompleted).length;
  $('remaining').textContent = tasks.filter(t => !t.isCompleted).length;
  $('save').disabled = busy;
}
async function load() { tasks = await request('/api/tasks'); render(); }
async function mutate(action) {
  if (busy) return; busy = true; render(); message('Сохранение…');
  try { await action(); await load(); message('Изменения сохранены.'); }
  catch (e) { message(e.message || 'Не удалось связаться с сервером.', true); }
  finally { busy = false; render(); }
}
function reset() { editing = null; $('task-form').reset(); $('form-title').textContent = 'Новая задача'; $('save').textContent = 'Добавить задачу'; $('cancel').hidden = true; }
$('task-form').addEventListener('submit', event => {
  event.preventDefault();
  const task = { title: $('title').value.trim(), subject: $('subject').value.trim(), dueDate: $('due').value || null, isCompleted: editing?.isCompleted || false };
  const id = editing?.id;
  mutate(async () => { await request(id ? `/api/tasks/${id}` : '/api/tasks', { method: id ? 'PUT' : 'POST', body: JSON.stringify(task) }); reset(); });
});
$('cancel').addEventListener('click', reset); $('filter').addEventListener('change', render);
load().catch(e => message(e.message || 'Не удалось загрузить список.', true));
