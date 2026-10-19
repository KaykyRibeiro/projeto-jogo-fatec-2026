# Verificação do jogo

## Procedimento reproduzível

1. Na Unity, use **Vale das Relíquias → Validar regras e cenas** para verificar contadores, parâmetros de animação, metas, recursos e referências de cada cena.
2. Compile uma versão de desenvolvimento para Windows.
3. Execute `ValeDasReliquias.exe --verify-game CAMINHO_ABSOLUTO_DOS_RESULTADOS` para ativar a verificação automatizada. Esse modo só está presente em compilações de desenvolvimento ou no editor e fica inativo numa partida comum.

A verificação aciona botões reais da interface, injeta eventos de teclado/mouse no Input System, exercita o CharacterController e testa contatos físicos com coletáveis e portas. Usa teletransporte entre pontos de teste para reduzir o tempo do percurso. Portanto, não equivale a uma pessoa caminhar continuamente por cada metro do terreno.

São verificadas entrada/saída das seis animações, coleta única e quotas, transição de cenas, alcance e bloqueio das portas, dano/cura, preservação do kit com vida cheia, derrota, restauração no reinício, vitória, nova partida e retorno ao menu a partir da pausa. Capturas e um relatório JSON são gravados na pasta indicada.

## Resultado observado

Verificação concluída em **19/10/2026**, com Unity **6000.3.10f1**, URP **17.3.0**, Input System **1.18.0** e Cinemachine **3.1.2**.

| Verificação | Resultado |
| --- | --- |
| Regras, recursos e referências das quatro cenas | 63 verificações aprovadas |
| Compilação Windows 64 bits, desenvolvimento | Sucesso, zero erros |
| Executável em 1600 × 900, proporção 16:9 | 55 verificações aprovadas; zero erros/exceções registrados; saída do processo 0 |
| Executável em 1280 × 960, proporção 4:3 | 55 verificações aprovadas; zero erros/exceções registrados; saída do processo 0 |
| Inspeção das capturas de menu, opções, fases, pausa e resultados | Interface legível nas duas proporções; terreno e materiais renderizados |

Os botões Opções, Voltar e Jogar foram acionados por eventos de pressão e soltura do mouse sobre suas posições na tela. Os demais botões da sequência foram acionados pelos respectivos eventos de clique da interface. Movimento, salto, câmera, ações e pausa usaram eventos do Input System; a coleta e a passagem pelas portas usaram os contatos físicos do personagem.

Os relatórios desta execução estão preservados em `Resultados/`: `recursos.txt`, `compilacao.txt`, `execucao-1600x900.json` e `execucao-1280x960.json`. As imagens em `Imagens/` são capturas do executável, distintas da ilustração conceitual.

### Limites da verificação

A sequência automatizada usa teletransporte entre pontos e não substitui uma sessão contínua de exploração feita pelo aluno. Não houve medição formal de desempenho nem avaliação auditiva dos efeitos; foram verificados os arquivos de áudio, suas chamadas e o ajuste real do volume. O encerramento do processo foi solicitado pelo verificador, não pelo botão Sair. O player em modo de teste oculto emitiu um aviso de liberação de ComputeBuffer ao encerrar a renderização, sem impedir os testes ou gerar exceções.

Capturas da tela inteira do editor, vídeos e arquivos de entrega exigidos pelo professor continuam sendo provas acadêmicas separadas, conforme `Aulas.md`.
