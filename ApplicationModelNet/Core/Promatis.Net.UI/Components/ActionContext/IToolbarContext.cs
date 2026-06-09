namespace Promatis.Net.UI.Components;

public interface IToolbarContext
{
    // --- ПАМЯТЬ КЛАССА ---
    Lock ControlsLock { get; }
    List<IUiControl> InnerControls { get; }
    bool IsToolbarInitialized { get; set; }

    // --- СВЯЗЬ С ЯДРОМ РЕАКТИВНОСТИ ---
    void NotifyContextUpdated();

    // --- ЧИСТЫЙ МЕТОД СБОРКИ (Должен только наполнять список!) ---
    void PopulateDefaultToolbar(List<IUiControl> controls);

    /// <summary>
    /// Единственная точка безопасного чтения кнопок визуальным слоем.
    /// Лениво собирает стартовый пакет ОДИН раз.
    /// </summary>
    public IEnumerable<IUiControl> Controls
    {
        get
        {
            lock (ControlsLock)
            {
                if (!IsToolbarInitialized)
                {
                    // Передаем сырой мутабельный список в класс-наследник.
                    // Наследник наполнит его без вызова триггеров и блокировок!
                    PopulateDefaultToolbar(InnerControls);
                    IsToolbarInitialized = true;
                }
                return InnerControls.ToArray(); // Безопасный изолированный снапшот
            }
        }
    }

    /// <summary>
    /// Динамическое добавление кнопок ПОСЛЕ того, как начальная инициализация завершена.
    /// </summary>
    public void AddControl(IUiControl control)
    {
        if (control == null) throw new ArgumentNullException(nameof(control));

        lock (ControlsLock)
        {
            // Форсируем сборку базы, если кто-то вызвал AddControl до первого рендера UI
            if (!IsToolbarInitialized)
            {
                PopulateDefaultToolbar(InnerControls);
                IsToolbarInitialized = true;
            }
            InnerControls.Add(control);
        }
        NotifyContextUpdated(); // Пинаем UI-поток только при динамических мутациях!
    }

    /// <summary>
    /// Динамическое удаление кнопок по ID.
    /// </summary>
    public void RemoveControl(string controlId)
    {
        if (string.IsNullOrWhiteSpace(controlId)) return;

        lock (ControlsLock)
        {
            if (!IsToolbarInitialized)
            {
                PopulateDefaultToolbar(InnerControls);
                IsToolbarInitialized = true;
            }
            InnerControls.RemoveAll(c => c.Id == controlId);
        }
        NotifyContextUpdated();
    }
}