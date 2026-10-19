# Vale das Relíquias

Jogo de exploração e coleta em terceira pessoa, desenvolvido na Unity para o projeto da Fatec. Aproveita o personagem Y Bot e os Starter Assets já existentes e reúne as atividades de coleta, portas, vida, menus e terreno em três fases.

## Abrir e jogar

1. No Unity Hub, abra a pasta `GameKayky2026` com **Unity 6000.3.10f1**.
2. Abra a cena `Assets/Scenes/Menu.unity`.
3. Entre em Play e clique em **Jogar**.

A versão para Windows fica em `Builds/Windows/ValeDasReliquias.exe`. Para copiar o jogo para outro computador, copie a pasta Windows inteira, incluindo a pasta de dados e as DLLs ao lado do executável.

## Controles

| Tecla | Ação |
| --- | --- |
| WASD ou setas | Andar |
| Shift | Correr |
| Espaço | Pular |
| Mouse | Controlar câmera |
| E | Abrir ou fechar a porta próxima |
| F | Chutar |
| 1 | Primeira dança |
| 2 | Headspin |
| 3 | Gangnam Style |
| 4 | Segunda dança |
| 5 | Acenar |
| ESC | Pausar ou continuar; voltar das opções |

As ações de animação funcionam com o personagem parado e no chão, enquanto a tecla estiver pressionada. O chute é uma demonstração de animação. As opções ajustam de fato o volume e a sensibilidade da câmera e são lembradas entre sessões.

## As três fases

- **O pátio das relíquias:** colete duas moedas, dois diamantes e duas chaleiras. A coleção completa leva à próxima fase.
- **Os portões do vale:** abra a porta livre com E, encontre a chave dourada e abra o portão trancado. Passe pelo arco de saída.
- **A última travessia:** colete duas peças de cada tipo e atravesse o arco verde. Cada bomba tira 25 de vida; cada maleta recupera 25, até o máximo de 100.

Vida zero leva à derrota. Reiniciar a fase restaura o mundo e a vida com que a fase começou. Jogar novamente depois da vitória inicia uma nova partida. Pausa e telas de resultado bloqueiam movimento, câmera e coleta.

## Editar o projeto

Os sistemas novos ficam em `GameKayky2026/Assets/_Game`:

- `Scripts`: sessão, regras, coleta, portas, interface e verificação de desenvolvimento.
- `Prefabs`: modelos reutilizáveis, portas e jogador.
- `Meshes`: malhas criadas para moeda, diamante, chaleira e vegetação.
- `Animations`: controlador corrigido do jogador e clipes dos objetos/portas.
- `Art/Professor`: recursos recebidos no ZIP.
- `Art/Surfaces`: doze conjuntos de materiais PBR e mapas preparados para URP.
- `Art/Generated`: imagem conceitual do vale, usada como referência de planejamento.
- `Materials` e `Terrain`: materiais, camadas e dados de relevo.
- `Editor`: montagem reproduzível e validação dos recursos.

Cada cena tem um objeto **Regras da fase**. Nele é possível ajustar objetivos, metas, sons e próxima cena. Os objetos **Cenário** e **Interface** são editáveis na Hierarchy. O tamanho e as metas podem ser personalizados sem alterar a lógica dos contadores.

O menu **Vale das Relíquias** no editor permite reconstruir as fases, validar os recursos e compilar Windows. Reconstruir substitui as cenas geradas e deve ser usado com atenção após personalizações manuais.

## Materiais e autoria

- Personagem Y Bot e animações: recursos Mixamo presentes no projeto original.
- Movimento: Unity Starter Assets; foram acrescentados controle de pausa, sensibilidade, chute na tecla F e limite correto de velocidade de queda.
- Chave, bomba, maleta de vida, imagens de interface, fonte e sons: materiais fornecidos pelo professor.
- Moeda personalizada, diamante, chaleira, portas, cenário e sistemas novos: produzidos neste projeto.
- Materiais PBR: [ambientCG](https://ambientcg.com/), CC0 1.0. Os identificadores e URLs estão em `Assets/_Game/Art/Surfaces/Sources.json`.
- Imagem de referência: gerada por IA; o registro do prompt está em `Docs/Referencia-visual.md`. Ela é uma ilustração de planejamento, não uma captura do jogo.

Os modelos novos são malhas/prefabs e animações Unity. Um arquivo nativo `.max` ou provas de modelagem no 3ds Max são entregas acadêmicas separadas.

## Verificação

O guia de verificação e os resultados observados ficam em `Docs/Verificacao.md`: passaram 63 verificações de recursos e 55 verificações de execução em cada uma das resoluções 1600 × 900 e 1280 × 960. A relação com as atividades do professor está em `Docs/Aulas.md`. Relatórios e algumas capturas foram preservados em `Docs`; logs, capturas intermediárias e compilações locais são ignorados pelo Git.
