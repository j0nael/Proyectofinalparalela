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
