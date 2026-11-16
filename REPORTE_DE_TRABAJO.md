# Reporte de trabajo — TaskManager API

Proyecto desarrollado con asistencia de IA (Claude Code), que escribió el código, ejecutó las pruebas y versionó cada módulo con git bajo la dirección y revisión del desarrollador.

## Prompt inicial

> Vas a crear MVP de una API REST de gestionar tareas. Cada tarea con titulo, descripcion, estado (enum pendiente, en curso, hecha), responsable y fecha limite. Para una primera version haz el CRUD completo de tareas con validaciones y mapeos entre DTO y modelos usando Mapper. Como stack tecnologico vas a usar  .NET  9, EF conectado a SQLSERVER. Para la validacion vas a crear minimo de 3 pruebas orientadas a la creacion, edicion y eliminacion de estas principalmente, el codigo lo haces en ingles y versiona con git cada modulo funcional que realices.

## Cambios y correcciones

### 1. Cambio de AutoMapper a Mapster

La primera versión del mapeo usaba AutoMapper 14.0.0, la última versión gratuita. Esa versión tiene 3 vulnerabilidades de gravedad alta que permiten tumbar la API (GHSA-6vhm-j9h7-f9gh, GHSA-gpwp-5797-mjff, GHSA-rvv3-g6hj-g44x), y las versiones corregidas requieren licencia comercial. Por eso se reemplazó por **Mapster** (gratuito, MIT), con aprobación del desarrollador.

### 2. Swagger para documentar y probar la API

**Petición:** *"Configura swagger y añadelo en el launchSettings para que levante navegador en la url indicada cuando se inicie la api"*

- **Interfaz**: Swagger UI disponible en `/swagger`, construida sobre el documento OpenAPI de .NET 9 (`/openapi/v1.json`).
- **Navegador**: `launchSettings.json` abre el navegador en `/swagger` al iniciar la API.
- **Estados**: se muestran por su nombre (`Pending`, `InProgress`, `Done`).

### 3. Estado como enum guardado en entero

**Petición:** *"El estado de las tareas debe ser enumerativo, y en la base de datos se debe manejar con numeros enteros"*

- **Columna**: `State` pasó de texto a `int` (`Pending = 0`, `InProgress = 1`, `Done = 2`). En el código sigue siendo el enum `TaskState`.
- **Restricción**: la base de datos solo acepta valores válidos del enum.
- **Migración**: convierte los datos existentes sin pérdida.
- **Tests**: se añadieron pruebas que verifican el tipo de columna y la restricción.

## Resultado

- **Tiempo**: 35 minutos en total, 15 de construcción y 20 de revisiones y correcciones.
- **Funcionalidad**: CRUD completo de tareas con validaciones y mapeo DTO ⇄ modelo.
- **Pruebas**: 13 tests (creación, edición, eliminación, mapeo y persistencia), todos pasan.
