# Projeto LeadLists

## Pré-requisitos

Antes de começar, garanta que você tenha as seguintes ferramentas instaladas e configuradas em seu ambiente:

#### Backend e Infraestrutura

- [.NET SDK 9.0+](https://dotnet.microsoft.com/download)
- [Docker e Docker Compose](https://docs.docker.com/get-docker/)
- [k3d](https://www.google.com/search?q=https://k3d.io/v5.6.0/%23install)    
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- (Opcional) [Lens](https://k8slens.dev/) para uma visualização gráfica do cluster.
    

#### Frontend

- [Node.js 20+](https://nodejs.org/en/download)
- NPM ou Yarn

---

## Guia de Execução

O projeto pode ser executado de duas maneiras: localmente com Docker Compose ou em um cluster Kubernetes com k3d.

### Localmente com Docker Compose

Esta é a melhor forrma para desenvolver e testar a API, o Worker e o Frontend.

#### Iniciar a Infraestrutura

O comando abaixo irá iniciar os contêineres do **Postgres** e **RabbitMQ** em background.

antes de executar, configurer o arquivo `.env` na raiz do projeto, se necessário.

```zsh
cp .env.example .env
```

`.env.example`:

```env
# PostgreSQL
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=<db_name>
POSTGRES_USER=<user>
POSTGRES_PASSWORD=<pass>

# RabbitMQ
RABBITMQ_HOST=localhost
RABBITMQ_USER=guest
RABBITMQ_PASS=guest
```

Agora, inicie os serviços com Docker Compose:

```zsh
docker-compose up -d
```

#### API

Navegue até o diretorio da API, aplique as migrações do banco de dados e inicie a aplicação.

```zsh
cd API/LeadListAPI

# Aplica as migrações do Entity Framework no banco de dados
dotnet ef database update

# Inicia a API
dotnet watch run
```

A documentação da API estará disponível em: `http://localhost:5267/swagger`

#### Worker

O Worker é o serviço que processa as listas em background. Assim que uma leadList é criada um novo worker roda.
Para rodar o worker localmente, mude a variavel `jobRunner`  no   arquivo `appsettings.Development.json` para `Local`.

#### Frontend

Para rodar o frontend, em outro terminal, inicie a aplicação React.


```zsh
cd frontend/lead-lists-frontend

# Instala as dependências
npm install
```
Antes de iniciar o frontend, certifique-se de que a API está rodando e atualize a variável `VITE_API_BASE_URL` no arquivo `.env` na raiz do projeto frontend, se necessário.

```zsh
cp .env.example .env
```

`.env.example`:

```zsh
VITE_API=http://localhost:8080
VITE_POLL_MS=30000
VITE_PAGE_SIZE=10
```

Agora, inicie o servidor de desenvolvimento:

```zsh
# Inicia o servidor de desenvolvimento
npm run dev
```

Acesse a aplicação em: `http://localhost:5173`

---

### Rodando no Kubernetes com k3d

Este modo simula um deploy completo em um ambiente de produção, orquestrando todos os serviços dentro de um cluster Kubernetes.


#### Criar o Cluster k3d

Este comando cria o cluster e mapeia as portas da API e do RabbitMQ para o seu `localhost`.


```zsh
k3d cluster create leadlists -p "8080:30080@loadbalancer" -p "15672:31567@loadbalancer"
```
#### Criar o Namespace

Este comando cria um namespace chamado `dev` para isolar os recursos da aplicação.

```zsh
kubectl create namespace dev
```

#### Build e Importação das Imagens

> [!INFO] 
> As imagens já estão prontas e publicadas no Docker Hub, caso queira buildar e importar a imagem local, mude a origem das imagens no `K8s/api/deployment.yaml`.


O k3d roda em seu próprio ambiente Docker e não tem acesso direto às imagens que você constrói localmente. Portanto, primeiro precisamos construir as imagens da API e do Worker e depois importá-las para o cluster.

```zsh
# Construir a imagem da API
docker build -t leadlist-api:local ./API/LeadListAPI

# Construir a imagem do Worker
docker build -t leadlist-worker:local ./worker
```

Agora, importe as imagens.

```zsh
k3d image import leadlist-api:local -c leadlists
k3d image import leadlist-worker:local -c leadlists
```


#### Aplicar os Manifestos do Kubernetes

Com o cluster criado e as imagens importadas, aplique todos os manifestos de configuração.


```zsh
kubectl apply -f K8s/rabbitmq -n dev
kubectl apply -f K8s/postgres -n dev
kubectl apply -f K8s/api -n dev

```

Este comando irá configurar toda a aplicação no namespace `dev`, incluindo:

- **PostgreSQL:** `StatefulSet`, `Service` e `Secret`.
- **RabbitMQ:** `Deployment`, `Services`, `Secret` e `ConfigMap`.
- **API:** `Deployment`, `Service` e `RBAC` (permissões).
    
#### Verificação

Para verificar se todos os pods estão rodando corretamente:


```zsh
kubectl get pods -n dev
```

Aguarde até que todos os pods estejam com o status `Running` e `1/1` na coluna `READY`.

Para ver os logs de um serviço:


```zsh
# Logs da API
kubectl logs -n dev -l app=api -f

# Logs do Worker
kubectl logs -n dev -l app=rabbitmq -f
```

---

### Testando o Fluxo de Ponta a Ponta

1. **Inicie o ambiente** (local ou k3d).
    
2. **Acesse o Frontend** (`http://localhost:5173`) ou a **API via Swagger** (`http://localhost:8080/swagger` para k3d, `http://localhost:5267/swagger` para local).
    
3. **Crie uma nova LeadList** através da interface ou enviando uma requisição `POST`.
    
4. **Observe o Status:** Acompanhe o status da lista, que deve transitar de `Pending` para `Processing` e, finalmente, para `Completed` ou `Failed`.
    
5. **Verifique a Fila de Falhas:** Em caso de falha, acesse a **UI do RabbitMQ** (`http://localhost:15672`) e verifique se uma mensagem apareceu na fila `leadlist.failed`.
    

### Gerenciamento do Cluster

Para destruir o cluster k3d e todos os seus recursos, execute:


```zsh
k3d cluster delete leadlists
```
