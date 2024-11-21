using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Collections.Generic;

namespace WebConsole.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConsoleController : ControllerBase
    {
        private readonly ILogger<ConsoleController> _logger;

        // Конструктор для инициализации логгера
        public ConsoleController(ILogger<ConsoleController> logger)
        {
            _logger = logger;
        }

        // Метод для выполнения команды
        [HttpPost]
        public IActionResult ExecuteCommand([FromBody] CommandRequest request)
        {
            string command = request.Command.Trim(); // Получаем команду из запроса и удаляем пробелы
            string output = ""; // Переменная для хранения вывода команды
            string currentDirectory = HttpContext.Session.GetString("CurrentDirectory") ?? "/app"; // Получаем текущий каталог из сессии или устанавливаем его в текущий каталог приложения

            try
            {
                if (command.StartsWith("cd ")) // Проверяем, является ли команда командой смены каталога
                {
                    string newDirectory = command.Substring(3).Trim(); // Получаем новый каталог из команды
                    if (Directory.Exists(newDirectory)) // Проверяем, существует ли новый каталог
                    {
                        currentDirectory = newDirectory; // Обновляем текущий каталог
                        output = $"Changed directory to {currentDirectory}"; // Устанавливаем вывод команды
                    }
                    else
                    {
                        output = $"Directory {newDirectory} does not exist"; // Устанавливаем вывод команды, если каталог не существует
                    }
                }
                else
                {
                    var process = new Process // Создаем новый процесс для выполнения команды
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "bash", // Устанавливаем имя файла для выполнения команды
                            Arguments = $"-c \"cd {currentDirectory} && {command}\"", // Устанавливаем аргументы для выполнения команды
                            RedirectStandardOutput = true, // Перенаправляем стандартный вывод
                            RedirectStandardError = true, // Перенаправляем стандартный вывод ошибок
                            UseShellExecute = false, // Не используем оболочку для выполнения команды
                            CreateNoWindow = true // Не создаем окно для процесса
                        }
                    };

                    process.Start(); // Запускаем процесс
                    output = process.StandardOutput.ReadToEnd(); // Читаем стандартный вывод процесса
                    string errorOutput = process.StandardError.ReadToEnd(); // Читаем стандартный вывод ошибок процесса
                    process.WaitForExit(); // Ожидаем завершения процесса

                    if (!string.IsNullOrEmpty(errorOutput)) // Проверяем, есть ли ошибки в выводе
                    {
                        output += "\n" + errorOutput; // Добавляем ошибки в вывод
                    }
                }

                // Сохраняем текущий каталог в сессии
                HttpContext.Session.SetString("CurrentDirectory", currentDirectory);

                // Сохраняем команду и вывод в историю
                var history = HttpContext.Session.GetObject<List<string>>("CommandHistory") ?? new List<string>(); // Получаем историю команд из сессии или создаем новую, если она не существует
                history.Add($"{currentDirectory} > {command}"); // Добавляем команду в историю
                history.Add(output); // Добавляем вывод команды в историю
                HttpContext.Session.SetObject("CommandHistory", history); // Сохраняем историю команд в сессии
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing command: {Command}", command); // Логируем ошибку
                output = $"Error executing command: {ex.Message}"; // Устанавливаем вывод команды в случае ошибки
            }

            return Ok(new { output, currentDirectory }); // Возвращаем вывод команды и текущий каталог
        }

        // Метод для получения истории команд
        [HttpGet("history")]
        public IActionResult GetCommandHistory()
        {
            var history = HttpContext.Session.GetObject<List<string>>("CommandHistory") ?? new List<string>(); // Получаем историю команд из сессии или создаем новую, если она не существует
            return Ok(history); // Возвращаем историю команд
        }
    }

    // Класс для представления запроса команды
    public class CommandRequest
    {
        public string Command { get; set; }
    }

    // Статический класс для расширения методов сессии
    public static class SessionExtensions
    {
        // Метод для сохранения объекта в сессии
        public static void SetObject(this ISession session, string key, object value)
        {
            session.Set(key, JsonSerializer.SerializeToUtf8Bytes(value));
        }

        // Метод для получения объекта из сессии
        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.Get(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
