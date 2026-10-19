# Diagnóstico Técnico y Guía de Solución: Monkchess2D (Unity 6.3 LTS / 6000.3.26f1)

> **Propósito de este documento:**  
> Este informe contiene el diagnóstico técnico exacto de por qué el juego **no se visualiza** y **no se puede editar** en el editor de Unity 6.3 LTS. Está estructurado con rutas de archivos, líneas exactas y soluciones recomendadas paso a paso para que una IA o desarrollador proceda con las correcciones.

---

## 1. Contexto del Proyecto
* **Versión del Editor:** Unity 6 LTS (`6000.3.26f1`)
* **Género / Tipo:** Juego de Ajedrez 2D
* **Paquetes Relevantes:** Universal Render Pipeline (`com.unity.render-pipelines.universal@17.3.0`), 2D Feature, UGUI.
* **Escenas Principales:** 
  * `Assets/Scenes/start.unity` (Portada / Menú)
  * `Assets/Scenes/Game.unity` (Partida / Tablero)

---

## 2. Diagnóstico: ¿Por qué NO SE VE el juego?

### A. Zoom de la Cámara Descalibrado (`orthographic size: 50`)
* **Archivo afectado:** `Assets/Scenes/Game.unity` (Línea 424)
* **Causa raíz:**  
  La `Main Camera` tiene configurado `orthographic size: 50` (anteriormente era `5`).  
  El tablero de ajedrez mide aproximadamente `4.6 x 4.6` unidades en el espacio del mundo (según `player.SetCoords()` en `Assets/scripts/player.cs`, donde las posiciones van de `x: -2.3` a `+2.3`). Con un tamaño ortográfico de 50, la cámara abarca un campo visual vertical de **100 unidades** y horizontal de **~178 unidades**.
* **Efecto:** El tablero y las piezas se reducen al tamaño de un punto diminuto casi imperceptible en el centro de la pantalla; parece que no hay nada cargado.
* **Solución requerida:**
  * En `Assets/Scenes/Game.unity`, restaurar `orthographic size` de la cámara a `5`.
  * Opcional: Ajustar el color de fondo `m_BackGroundColor` (línea 399) de magenta a negro/color deseado.

---

### B. Las Piezas NO existen en la escena en Modo Editor
* **Archivo afectado:** `Assets/scripts/Game.cs` (Líneas 22–76)
* **Causa raíz:**  
  Las 32 piezas de ajedrez no están colocadas como GameObjects en la escena `Game.unity`. Se instancian dinámicamente mediante `Create()` -> `Instantiate(chesspiece, ...)` en el método `Start()` de `Game.cs` al presionar **Play**.
* **Efecto:** Al abrir `Game.unity` en el editor, la escena parece vacía (solo se ve el tablero base y la UI). No hay piezas visibles ni seleccionables hasta que se inicia la simulación.
* **Solución requerida:**
  * Entender que para ver y probar las piezas se debe presionar **Play**.
  * Si se desea ver las piezas en modo diseño (sin presionar Play), se deben colocar directamente en la escena o utilizar un script con `[ExecuteInEditMode]`.

---

### C. Conflicto de Render Pipeline (URP 2D vs Built-in)
* **Archivos afectados:**
  * `ProjectSettings/GraphicsSettings.asset` (Línea 40: `m_CustomRenderPipeline: {fileID: 0}`)
  * `ProjectSettings/QualitySettings.asset` (Línea 51: `customRenderPipeline: {fileID: 0}`)
  * `Assets/Settings/UniversalRP.asset` (GUID: `681886c5eb7344803b6206f758bf0b1c`)
  * `Assets/prefab/king1.prefab` (Línea 57: Material Lit URP)
  * `Assets/Scenes/Game.unity` (Línea 545: `Global Light 2D`)
* **Causa raíz:**  
  El proyecto contiene configuración y componentes para **Universal Render Pipeline (URP 2D)** (`Global Light 2D`, `UniversalAdditionalCameraData`, shaders `Sprite-Lit-Default`). Sin embargo, en `GraphicsSettings` y `QualitySettings`, el campo `Custom Render Pipeline` está vacío (`None`). Unity 6 ejecuta el renderizador clásico (Built-in), el cual **ignora las luces 2D** y puede provocar que los sprites con materiales Lit se vean negros, sin iluminación o con errores de sombreador.
* **Solución requerida:**
  * Asignar el asset `UniversalRP.asset` (`guid: 681886c5eb7344803b6206f758bf0b1c`) en `GraphicsSettings.asset` bajo `m_CustomRenderPipeline`.
  * Asignarlo también en cada tier de calidad en `QualitySettings.asset`.
  * Alternativamente, si se prefiere Built-in, cambiar los materiales de los sprites a `Sprites/Default` (Unlit) y retirar los componentes de URP.

---

## 3. Diagnóstico: ¿Por qué NO DEJA EDITAR ni interactuar?

### A. Imposibilidad de seleccionar o mover piezas en la escena
* **Causa raíz:**  
  Como las piezas son instanciadas por código en tiempo de ejecución, no existen en la ventana **Hierarchy** ni en la **Scene View** de Unity en modo edición.
* **Solución requerida:**
  * Para editar los sprites, colliders, escala o componentes de las piezas, se debe abrir y editar directamente el prefab base:  
    `Assets/prefab/king1.prefab`
  * Para editar el plato de movimiento:  
    `Assets/prefab/MovePlate.prefab`

---

### B. Tags no registrados en el proyecto
* **Archivos afectados:**
  * `ProjectSettings/TagManager.asset`
  * `Assets/scripts/Game.cs`
  * `Assets/scripts/player.cs`
* **Causa raíz:**  
  El código C# hace búsquedas estrictas por Tag:
  * `GameObject.FindGameObjectWithTag("GameController")`
  * `GameObject.FindGameObjectWithTag("turn1")`
  * `GameObject.FindGameObjectWithTag("turn2")`
  * `GameObject.FindGameObjectWithTag("win")`
  * `GameObject.FindGameObjectWithTag("whitewon")`
  * `GameObject.FindGameObjectWithTag("blackwon")`
  * `GameObject.FindGameObjectWithTag("restart")`
  * `GameObject.FindGameObjectsWithTag("MovePlate")`
  
  Sin embargo, en `ProjectSettings/TagManager.asset`, el único tag registrado en la lista es `MovePlate`.
* **Efecto:** Unity 6 puede desasignar los tags no registrados o arrojar `NullReferenceException` inmediatamente al iniciar o cambiar de turno en `Game.Nextturn()`.
* **Solución requerida:**
  * Agregar todos los tags requeridos a la lista `tags` en `ProjectSettings/TagManager.asset`:
    ```yaml
    tags:
    - MovePlate
    - GameController
    - turn1
    - turn2
    - win
    - whitewon
    - blackwon
    - restart
    - gameover
    ```

---

### C. Build Settings sin escenas registradas
* **Archivo afectado:** `ProjectSettings/EditorBuildSettings.asset` (Línea 7: `m_Scenes: []`)
* **Causa raíz:**  
  La lista de escenas en Build Settings está vacía. Al ejecutar `Assets/Scenes/start.unity`, el script `starting.cs` ejecuta `SceneManager.LoadScene("Game")` tras el primer clic.
* **Efecto:** Unity lanza un error en consola indicando que la escena `"Game"` no existe en Build Settings, impidiendo entrar a la partida.
* **Solución requerida:**
  * Registrar las dos escenas en `EditorBuildSettings.asset`:
    1. `Assets/Scenes/start.unity` (Índice 0)
    2. `Assets/Scenes/Game.unity` (Índice 1)

---

## 4. Plan de Acción Recomendado para la IA que lo solucionará

1. **Paso 1: Restaurar la cámara en `Game.unity`**
   * Cambiar `orthographic size` de `50` a `5`.
   * Verificar que la posición sea `{x: 0, y: 0, z: -10}`.

2. **Paso 2: Registrar Tags en `ProjectSettings/TagManager.asset`**
   * Añadir `GameController`, `turn1`, `turn2`, `win`, `whitewon`, `blackwon`, `restart`, `gameover`.

3. **Paso 3: Añadir escenas a `ProjectSettings/EditorBuildSettings.asset`**
   * Agregar `Assets/Scenes/start.unity` y `Assets/Scenes/Game.unity`.

4. **Paso 4: Enlazar el Render Pipeline URP**
   * Asignar `UniversalRP.asset` (`guid: 681886c5eb7344803b6206f758bf0b1c`) en `GraphicsSettings.asset` y `QualitySettings.asset`.

5. **Paso 5: Pruebas de verificación**
   * Abrir `Game.unity` en Unity Editor y verificar que el tablero ocupe la pantalla.
   * Darle a **Play** y comprobar que las piezas aparezcan iluminadas en sus casillas y respondan a los clics.

