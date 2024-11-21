let currentDirectory = '';

// Функция для обновления приглашения командной строки
function updatePrompt() {
    const prompt = document.getElementById('prompt');
    prompt.innerHTML = `${currentDirectory} > <input type="text" id="command" autofocus />`;
    document.getElementById('command').focus();
}

// Функция для выполнения команды
function executeCommand(command) {
    const output = document.getElementById('output');

    // Добавляем команду в вывод
    const commandElement = document.createElement('div');
    commandElement.textContent = `${currentDirectory} > ${command}`;
    output.appendChild(commandElement);

    // Отправляем команду на сервер
    fetch('/api/console', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ command: command })
    })
        .then(response => response.json())
        .then(data => {
            // Разбиваем ответ на строки и добавляем каждую строку отдельно
            const responseLines = data.output.split('\n');
            responseLines.forEach(line => {
                const responseElement = document.createElement('div');
                responseElement.textContent = line;
                output.appendChild(responseElement);
            });

            // Обновляем текущий каталог
            currentDirectory = data.currentDirectory;

            // Обновляем приглашение
            updatePrompt();
        })
        .catch(error => {
            console.error('Error:', error);
            // Обновляем приглашение даже в случае ошибки
            updatePrompt();
        });
}

// Функция для загрузки истории команд
function loadCommandHistory() {
    fetch('/api/console/history')
        .then(response => response.json())
        .then(data => {
            const output = document.getElementById('output');
            data.forEach(line => {
                const historyElement = document.createElement('div');
                historyElement.textContent = line;
                output.appendChild(historyElement);
            });
        })
        .catch(error => {
            console.error('Error:', error);
        });
}

document.addEventListener('DOMContentLoaded', function () {
    currentDirectory = document.getElementById('current-directory').textContent;
    updatePrompt();
    loadCommandHistory();

    document.getElementById('command').addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            const command = e.target.value;
            executeCommand(command);
        }
    });
});