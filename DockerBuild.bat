docker build -t szawaski/zerrademo-weatherdomain -f Demo/ZerraDemo.Service.Weather/Dockerfile .
docker build -t szawaski/zerrademo-weathercacheddomain -f Demo/ZerraDemo.Service.WeatherCached/Dockerfile .
docker build -t szawaski/zerrademo-petsdomain -f Demo/ZerraDemo.Service.Pets/Dockerfile .
docker build -t szawaski/zerrademo-ledger2domain -f Demo/ZerraDemo.Service.Ledger2/Dockerfile .
docker build -t szawaski/zerrademo-ledger1domain -f Demo/ZerraDemo.Service.Ledger1/Dockerfile .
docker build -t szawaski/zerrademo-web -f Demo/ZerraDemo.Web/Dockerfile .