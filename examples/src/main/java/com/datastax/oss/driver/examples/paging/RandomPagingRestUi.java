/*
 * Copyright DataStax, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package com.datastax.oss.driver.examples.paging;

import com.datastax.oss.driver.api.core.CqlSession;
import com.datastax.oss.driver.api.core.CqlSessionBuilder;
import com.datastax.oss.driver.api.core.cql.PreparedStatement;
import com.datastax.oss.driver.api.core.cql.ResultSet;
import com.datastax.oss.driver.api.core.cql.Row;
import com.datastax.oss.driver.api.core.cql.Statement;
import com.datastax.oss.driver.internal.core.type.codec.DateCodec;
import com.sun.net.httpserver.HttpServer;
import java.io.IOException;
import java.net.URI;
import java.nio.ByteBuffer;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import javax.annotation.PostConstruct;
import javax.inject.Inject;
import javax.inject.Singleton;
import javax.ws.rs.GET;
import javax.ws.rs.Path;
import javax.ws.rs.PathParam;
import javax.ws.rs.Produces;
import javax.ws.rs.QueryParam;
import javax.ws.rs.core.Context;
import javax.ws.rs.core.UriBuilder;
import javax.ws.rs.core.UriInfo;
import org.glassfish.hk2.utilities.binding.AbstractBinder;
import org.glassfish.jersey.jdkhttp.JdkHttpServerFactory;
import org.glassfish.jersey.server.ResourceConfig;

/**
 * A stateless REST service (backed by <a href="https://jersey.java.net/">Jersey</a>, <a
 * href="https://hk2.java.net/">HK2</a> and the JDK HttpServer) that displays paginated results for
 * a CQL query.
 *
 * <p>Conversion to and from JSON is made through <a
 * href="https://jersey.java.net/documentation/latest/media.html#json.jackson">Jersey Jackson
 * providers</a>.
 *
 * <p>Navigation is bidirectional, and you can jump to a random page (by modifying the URL).
 * Cassandra does not support offset queries (see
 * https://issues.apache.org/jira/browse/CASSANDRA-6511), so we emulate it by restarting from the
 * beginning each time, and iterating through the results until we reach the requested page. This is
 * fundamentally inefficient (O(n) in the number of rows skipped), but the tradeoff might be
 * acceptable for some use cases; for example, if you show 10 results per page and you think users
 * are unlikely to browse past page 10, you only need to retrieve at most 100 rows.
 *
 * <p>Preconditions:
 *
 * <ul>
 *   <li>An Apache Cassandra(R) cluster is running and accessible through the contacts points
 *       identified by basic.contact-points (see application.conf).
 * </ul>
 *
 * <p>Side effects:
 *
 * <ul>
 *   <li>creates a new keyspace "examples" in the cluster. If a keyspace with this name already
 *       exists, it will be reused;
 *   <li>creates a table "examples.random_paging_rest_ui". If it already exists, it will be reused;
 *   <li>inserts data in the table;
 *   <li>launches a REST server listening on HTTP_PORT.
 * </ul>
 */
public class RandomPagingRestUi {
  private static final int HTTP_PORT = 8080;

  private static final int ITEMS_PER_PAGE = 10;
  // How many rows the driver will retrieve at a time.
  // This is set artificially low for the sake of this example.
  // Unless your rows are very large, you can probably use a much higher value (the driver's default
  // is 5000).
  private static final int FETCH_SIZE = 60;

  private static final URI BASE_URI =
      UriBuilder.fromUri("http://localhost/").path("").port(HTTP_PORT).build();

  public static void main(String[] args) throws Exception {

    try (CqlSession session = new CqlSessionBuilder().addTypeCodecs(new DateCodec()).build()) {
      createSchema(session);
      populateSchema(session);
      startRestService(session);
    }
  }

  // Creates a table storing videos by users, in a typically denormalized way
  private static void createSchema(CqlSession session) {
    session.execute(
        "CREATE KEYSPACE IF NOT EXISTS examples "
            + "WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1}");
    session.execute(
        "CREATE TABLE IF NOT EXISTS examples.random_paging_rest_ui("
            + "userid int, username text, "
            + "added timestamp, "
            + "videoid int, title text, "
            + "PRIMARY KEY (userid, added, videoid)"
            + ") WITH CLUSTERING ORDER BY (added DESC, videoid ASC)");
  }

  private static void populateSchema(CqlSession session) {
    PreparedStatement prepare =
        session.prepare(
            "INSERT INTO examples.random_paging_rest_ui (userid, username, added, videoid, title) VALUES (?, ?, ?, ?, ?)");

    // 3 users
    for (int i = 0; i < 3; i++) {
      // 49 videos each
      for (int j = 0; j < 49; j++) {
        int videoid = i * 100 + j;
        session.execute(
            prepare.bind(
                i, "user " + i, Instant.ofEpochMilli(j * 100000), videoid, "video " + videoid));
      }
    }
  }

  // starts the REST server using JDK HttpServer (com.sun.net.httpserver.HttpServer)
  private static void startRestService(CqlSession session)
      throws IOException, InterruptedException {

    final HttpServer server =
        JdkHttpServerFactory.createHttpServer(BASE_URI, new VideoApplication(session), false);
    final ExecutorService executor = Executors.newSingleThreadExecutor();
    server.setExecutor(executor);
    Runtime.getRuntime()
        .addShutdownHook(
            new Thread(
                () -> {
                  System.out.println();
                  System.out.println("Stopping REST Service");
                  server.stop(0);
                  executor.shutdownNow();
                  System.out.println("REST Service stopped");
                }));
    server.start();

    System.out.println();
    System.out.printf(
        "REST Service started on http://localhost:%d/users, press CTRL+C to stop%n", HTTP_PORT);
    System.out.println(
        "To explore this example, start with the following request and walk from there:");
    System.out.printf("curl -i http://localhost:%d/users/1/videos%n", HTTP_PORT);
    System.out.println();

    Thread.currentThread().join();
  }

  /**
   * Configures the REST application and handles injection of custom objects, such as the driver
   * session.
   *
   * <p>This is also the place where you would normally configure JSON serialization, etc.
   *
   * <p>Note that in this example, we rely on the automatic discovery and configuration of Jackson
   * through {@code org.glassfish.jersey.jackson.JacksonFeature}.
   */
  public static class VideoApplication extends ResourceConfig {

    public VideoApplication(final CqlSession session) {
      super(UserService.class);
      // AbstractBinder is provided by HK2
      register(
          new AbstractBinder() {

            @Override
            protected void configure() {
              bind(session).to(CqlSession.class);
            }
          });
    }
  }

  /**
   * A typical REST service, handling requests involving users.
   *
   * <p>Typically, this service would contain methods for listing and searching for users, and
   * methods to retrieve user details. Here, for brevity, only one method, listing videos by user,
   * is implemented.
   */
  @Singleton
  @Path("/users")
  @Produces("application/json")
  public static class UserService {

    @Inject private CqlSession session;

    @Context private UriInfo uri;

    private PreparedStatement videosByUser;
    private Pager pager;

    @PostConstruct
    @SuppressWarnings("unused")
    public void init() {
      this.pager = new Pager(session, ITEMS_PER_PAGE);
      this.videosByUser =
          session.prepare(
              "SELECT videoid, title, added FROM examples.random_paging_rest_ui WHERE userid = ?");
    }

    /**
     * Returns a paginated list of all the videos created by the given user.
     *
     * @param userid the user ID.
     * @param page the page to request, or {@code null} to get the first page.
     */
    @GET
    @Path("/{userid}/videos")
    public UserVideosResponse getUserVideos(
        @PathParam("userid") int userid, @QueryParam("page") Integer page) {

      Statement statement = videosByUser.bind(userid).setPageSize(FETCH_SIZE);

      if (page == null) {
        page = 1;
      }
      ResultSet rs = pager.skipTo(statement, page);

      List<UserVideo> videos;
      boolean empty = isExhausted(rs);
      if (empty) {
        videos = Collections.emptyList();
      } else {
        int remaining = ITEMS_PER_PAGE;
        videos = new ArrayList<>(remaining);
        for (Row row : rs) {
          UserVideo video =
              new UserVideo(row.getInt("videoid"), row.getString("title"), row.getInstant("added"));
          videos.add(video);

          if (--remaining == 0) {
            break;
          }
        }
      }

      URI previous =
          (page == 1) ? null : uri.getAbsolutePathBuilder().queryParam("page", page - 1).build();
      URI next = (empty) ? null : uri.getAbsolutePathBuilder().queryParam("page", page + 1).build();
      return new UserVideosResponse(videos, previous, next);
    }
  }

  private static boolean isExhausted(ResultSet rs) {
    return !rs.iterator().hasNext();
  }

  @SuppressWarnings("unused")
  public static class UserVideosResponse {

    private final List<UserVideo> videos;

    private final URI previousPage;

    private final URI nextPage;

    public UserVideosResponse(List<UserVideo> videos, URI previousPage, URI nextPage) {
      this.videos = videos;
      this.previousPage = previousPage;
      this.nextPage = nextPage;
    }

    public List<UserVideo> getVideos() {
      return videos;
    }

    public URI getPreviousPage() {
      return previousPage;
    }

    public URI getNextPage() {
      return nextPage;
    }
  }

  @SuppressWarnings("unused")
  public static class UserVideo {

    private final int videoid;

    private final String title;

    private final Instant added;

    public UserVideo(int videoid, String title, Instant added) {
      this.videoid = videoid;
      this.title = title;
      this.added = added;
    }

    public int getVideoid() {
      return videoid;
    }

    public String getTitle() {
      return title;
    }

    public Instant getAdded() {
      return added;
    }
  }

  /**
   * Helper class to emulate random paging.
   *
   * <p>Note that it MUST be stateless, because it is cached as a field in our HTTP handler.
   */
  static class Pager {
    private final CqlSession session;
    private final int pageSize;

    Pager(CqlSession session, int pageSize) {
      this.session = session;
      this.pageSize = pageSize;
    }

    ResultSet skipTo(Statement statement, int displayPage) {
      // Absolute index of the first row we want to display on the web page. Our goal is that
      // rs.next() returns that row.
      int targetRow = (displayPage - 1) * pageSize;

      ResultSet rs = session.execute(statement);
      // Absolute index of the next row returned by rs (if it is not exhausted)
      int currentRow = 0;
      int fetchedSize = rs.getAvailableWithoutFetching();
      ByteBuffer nextState = rs.getExecutionInfo().getPagingState();

      // Skip protocol pages until we reach the one that contains our target row.
      // For example, if the first query returned 60 rows and our target is row number 90, we know
      // we can skip those 60 rows directly without even iterating through them.
      // This part is optional, we could simply iterate through the rows with the for loop below,
      // but that's slightly less efficient because iterating each row involves a bit of internal
      // decoding.
      while (fetchedSize > 0 && nextState != null && currentRow + fetchedSize < targetRow) {
        statement = statement.setPagingState(nextState);
        rs = session.execute(statement);
        currentRow += fetchedSize;
        fetchedSize = rs.getAvailableWithoutFetching();
        nextState = rs.getExecutionInfo().getPagingState();
      }

      if (currentRow < targetRow) {
        for (@SuppressWarnings("unused") Row row : rs) {
          if (++currentRow == targetRow) {
            break;
          }
        }
      }
      // If targetRow is past the end, rs will be exhausted.
      // This means you can request a page past the end in the web UI (e.g. request page 12 while
      // there are only 10 pages), and it will show up as empty.
      // One improvement would be to detect that and take a different action, for example redirect
      // to page 10 or show an error message, this is left as an exercise for the reader.
      return rs;
    }
  }
}
