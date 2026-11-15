# FlowaOrders
Duas aplicações em C# que trocam ordens via protocolo FIX 4.4: o OrderGenerator envia ordens a partir de um formulário web, e o OrderAccumulator calcula a exposição financeira por símbolo e rejeita ordens que ultrapassem o limite de R$ 100.000.000.


# Tecnologias
- .NET 10 / API
- QuickFIX/n 1.14 (FIX 4.4)
- React 19


# Como rodar

```bash
# 1. OrderAccumulator (escuta FIX na porta 5001)
dotnet run --project src/OrderAccumulator

# 2. OrderGenerator API (http://localhost:5285)
dotnet run --project src/OrderGenerator.Api --launch-profile http

#3. Frontend (http://localhost:5173)
cd frontend
npm install
npm run dev
```

Abra http://localhost:5173 e envie uma ordem.


>  This is a challenge by [Coodesh](https://coodesh.com/)