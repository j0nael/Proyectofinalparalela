# Nodo Minero para una Cooperativa de Energía Solar

Proyecto final de **Programación Paralela** — demostración de paralelismo mediante
**descomposición exploratoria** aplicada a la minería de una blockchain que registra
los intercambios de energía de una cooperativa solar.

## ¿Qué hace?

Registra transacciones de energía entre casas (quién aporta y quién consume kWh) en
una blockchain, y **sella (mina)** cada bloque encontrando un *nonce* que haga que el
hash del bloque empiece con cierta cantidad de ceros. La búsqueda de ese nonce se
reparte entre varios hilos para demostrar la **ventaja del paralelismo**.

## Requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) o superior

## Cómo ejecutar

```bash
# 1. Ejecutar la demo (mina un bloque secuencial vs paralelo y compara)
dotnet run --project src/SolarMiner

# 2. Ejecutar el estudio completo de escalabilidad (genera el CSV de métricas)
dotnet run --project src/SolarMiner -- benchmark

# 3. Construir la cadena completa, verificarla y generar el visor HTML interactivo
dotnet run --project src/SolarMiner -- chain

# 4. Generar la página de reportes CSV por período (se abre en el navegador)
dotnet run --project src/SolarMiner -- reporte

# 5. Sellar la cadena A PARTIR DE LOS CSV de energía (flujo real completo)
#    Genera los CSV por período (si no existen), sella un bloque por cada CSV,
#    verifica la cadena y abre el visor. Período opcional: semanal | quincenal | mensual
dotnet run --project src/SolarMiner -- sellar quincenal

# 6. Ejecutar las pruebas
dotnet run --project tests/SolarMiner.Tests
```

## Estructura del repositorio

```
/docs      → Documentación del proyecto (documento principal, diagramas)
/src       → Código fuente (proyecto SolarMiner)
/tests     → Pruebas del sistema
/metrics   → Resultados de las comparativas (CSV, gráficas)
```

## Cómo funciona

- **Descomposición exploratoria**: el espacio de nonces (0, 1, 2, ...) se divide entre
  N hilos. Cada hilo prueba un subconjunto y todos buscan a la vez.
- **Sincronización**: cuando un hilo encuentra el nonce válido, activa un
  `CancellationToken` compartido ("cartel de ya lo encontré") y los demás paran.
  El contador global de hashes se actualiza con `Interlocked` para evitar condiciones
  de carrera.

  ## Equipo

- (Líder) — Jonathan Joel Geraldo Frias
- Integrante 2 — Jhonatan E. Romero
- Integrante 3 — Daniel Ureña Sanchez
